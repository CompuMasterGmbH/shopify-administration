using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ShopifyAdministration.Core;
using ShopifyAdministration.Platform;

static class Program
{
    static int assertions;
    static void Check(bool value, string label)
    {
        assertions++;
        if (!value) throw new Exception("FAILED: " + label);
    }
    static void Throws<T>(Action action, string label) where T : Exception
    {
        try { action(); } catch (T) { Check(true, label); return; }
        throw new Exception("FAILED: expected " + typeof(T).Name + ": " + label);
    }
    static async Task ThrowsAsync<T>(Func<Task> action, string label) where T : Exception
    {
        try { await action(); } catch (T) { Check(true, label); return; }
        throw new Exception("FAILED: expected " + typeof(T).Name + ": " + label);
    }
    static ConnectionSettings Profile(string host = "test-shop") => new ConnectionSettings
    {
        ApiBaseUrl = "https://" + host + ".myshopify.com/admin/api/2023-01/",
        ApiKey = "dummy-user", Password = "dummy-password-for-tests"
    };
    static HttpResponseMessage ShopResponse() => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"shop\":{\"name\":\"Test shop\"}}")
    };
    static async Task Main()
    {
        var normalized = Profile();
        normalized.ApiBaseUrl = " https://TEST-SHOP.myshopify.com/admin/api/2023-01 ";
        Check(normalized.Normalize().ApiBaseUrl == Profile().ApiBaseUrl, "normalize URL and trailing slash");
        foreach (var url in new[] {
            "http://test-shop.myshopify.com/admin/api/2023-01/",
            "https://test-shop.myshopify.com.evil.test/admin/api/2023-01/",
            "https://user:password@test-shop.myshopify.com/admin/api/2023-01/",
            "https://test-shop.myshopify.com/admin/api/2023-01/?token=x",
            "https://test-shop.myshopify.com/admin/api/2023-01/#secret",
            "https://test-shop.myshopify.com:444/admin/api/2023-01/",
            "https://test-shop.myshopify.com/admin/api/2023-02/",
            "https://test-shop.myshopify.com/", "not-a-url" })
        {
            var bad = Profile(); bad.ApiBaseUrl = url;
            Throws<ArgumentException>(() => bad.Normalize(), "reject unsafe/ambiguous configured URL");
        }
        var invalid = Profile(); invalid.Password = "";
        Throws<ArgumentException>(() => invalid.Normalize(), "password required");
        invalid = Profile(); invalid.ApiKey = "bad:key";
        Throws<ArgumentException>(() => invalid.Normalize(), "basic username colon rejected");
        invalid = Profile(); invalid.Password = "token\r\ninjected";
        Throws<ArgumentException>(() => invalid.Normalize(), "header line break rejected");
        var settings = Profile();
        var transport = new Handler((r, ct) =>
        {
            Check(r.RequestUri.Host == "test-shop.myshopify.com", "configured request destination");
            Check(r.Headers.Authorization.Scheme == "Basic", "legacy authentication supported");
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(r.Headers.Authorization.Parameter));
            Check(value == settings.ApiKey + ":" + settings.Password, "configured credentials used");
            return Task.FromResult(ShopResponse());
        });
        using (var api = new ShopifyApiClient(settings, transport))
        {
            settings.Password = "changed-after-client-creation";
            Check(api.ResolveRequestUri("products.json?limit=2").AbsolutePath.EndsWith("/products.json"), "relative endpoint resolves");
            foreach (var bad in new[] { "https://evil.test/admin/api/2023-01/products.json", "//evil.test/products.json",
                "http://test-shop.myshopify.com/admin/api/2023-01/products.json", "../2026-10/products.json",
                "/admin/products.json", "https://user@test-shop.myshopify.com/admin/api/2023-01/shop.json" })
                Throws<InvalidOperationException>(() => api.ResolveRequestUri(bad), "reject credential recipient change");
            // Construction cloned settings; use the original value in handler expectations.
            settings.Password = "dummy-password-for-tests";
            Check(await api.ValidateLoginAsync() == "Test shop", "validate authenticated shop response");
            Check(transport.Requests == 1, "rejected destinations never sent");
        }
        var tokenProfile = Profile(); tokenProfile.ApiKey = "";
        using (var api = new ShopifyApiClient(tokenProfile, new Handler((r, ct) =>
        {
            Check(r.Headers.Authorization == null, "token mode has no Basic header");
            Check(string.Join("", r.Headers.GetValues("X-Shopify-Access-Token")) == tokenProfile.Password, "token header sent");
            return Task.FromResult(ShopResponse());
        }))) await api.ValidateLoginAsync();

        foreach (var code in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.Redirect })
        {
            using var api = new ShopifyApiClient(Profile(), new Handler((r, ct) => Task.FromResult(new HttpResponseMessage(code))));
            await ThrowsAsync<InvalidOperationException>(() => api.ValidateLoginAsync(), "reject failed auth and redirects");
        }
        using (var api = new ShopifyApiClient(Profile(), new Handler((r, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }))))
            await ThrowsAsync<InvalidOperationException>(() => api.ValidateLoginAsync(), "HTML/invalid shop response not a login");
        var store = new MemoryStore();
        using (var session = new LoginSession(store, () => new Handler((r, ct) => Task.FromResult(ShopResponse()))))
        {
            await session.LoginAsync(Profile(), false);
            Check(store.Saves == 0 && store.Deletes == 1, "unchecked means no persistence and removes old profile");
            var previous = session.ActiveClient;
            session.Logout();
            Check(session.ActiveClient == null, "logout clears active session");
            Check(session.LastConnection.Password == Profile().Password, "logout retains last values for prefill");
            await ThrowsAsync<ObjectDisposedException>(() => previous.GetAsync("products.json"), "old account client cannot be used");
            await session.LoginAsync(Profile("another-shop"), true);
            Check(store.Saves == 1 && store.Value.ApiBaseUrl.Contains("another-shop"), "save new account only after validation");
            var snapshot = session.LastConnection;
            snapshot.Password = "mutated";
            Check(session.LastConnection.Password != "mutated", "prefill returns a copy");
            Check(session.LoadRememberedConnection().ApiBaseUrl.Contains("another-shop"), "saved profile available for automatic login");
            await session.LoginAsync(session.LastConnection, true);
            Check(session.ActiveClient != null, "saved profile automatic login validates");
            store.FailSave = true;
            var old = session.ActiveClient;
            await ThrowsAsync<IOException>(() => session.LoginAsync(Profile(), true), "failed save fails login visibly");
            Check(ReferenceEquals(old, session.ActiveClient), "failed replacement never replaces prior active session");
        }
        store = new MemoryStore();
        using (var session = new LoginSession(store, () => new Handler((r, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)))))
        {
            await ThrowsAsync<InvalidOperationException>(() => session.LoginAsync(Profile(), true), "invalid login rejected");
            Check(session.ActiveClient == null && store.Saves == 0, "failed login never saved or activated");
        }
        var fallback = new SessionOnlyCredentialStore();
        Check(!fallback.CanPersist && fallback.Load() == null, "unsupported platforms use session-only fallback");
        Throws<PlatformNotSupportedException>(() => fallback.Save(Profile()), "fallback cannot write plaintext");
        fallback.Delete();
        using (var session = new LoginSession(fallback, () => new Handler((r, ct) => Task.FromResult(ShopResponse()))))
        {
            await session.LoginAsync(Profile(), false);
            Check(session.ActiveClient != null, "fallback permits session-only authentication");
        }
        var pendingTransport = new Handler(async (r, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return ShopResponse();
        });
        var pendingClient = new ShopifyApiClient(Profile(), pendingTransport);
        var pending = pendingClient.GetAsync("products.json");
        pendingClient.Dispose();
        await ThrowsAsync<OperationCanceledException>(async () => { using var unused = await pending; }, "logout cancels in-flight work");
        if (OperatingSystem.IsWindows()) TestWindowsStore();
        else Check(CredentialStoreFactory.Create() is SessionOnlyCredentialStore, "factory guards Windows adapter");
        Console.WriteLine("PASS: " + assertions + " assertions (no live Shopify calls).");
    }
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    static void TestWindowsStore()
    {
        var directory = Path.Combine(Path.GetTempPath(), "shopify-login-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "credentials.v1.bin");
            var store = new WindowsCredentialStore(path);
            Check(store.CanPersist && store.Load() == null, "Windows store initially absent");
            store.Save(Profile());
            var bytes = File.ReadAllBytes(path);
            var raw = Encoding.UTF8.GetString(bytes);
            Check(!raw.Contains(Profile().Password) && !raw.Contains(Profile().ApiKey) && !raw.Contains(".myshopify.com"), "entire stored profile encrypted");
            var loaded = store.Load();
            Check(loaded.Password == Profile().Password && loaded.ApiBaseUrl == Profile().ApiBaseUrl, "DPAPI round-trip current user");
            store.Save(Profile("second-shop"));
            Check(store.Load().ApiBaseUrl.Contains("second-shop"), "atomic replacement round-trip");
            Check(Directory.GetFiles(directory).Length == 1, "no leftover plaintext or temporary file");
            bytes = File.ReadAllBytes(path); bytes[bytes.Length - 1] ^= 1; File.WriteAllBytes(path, bytes);
            Throws<InvalidOperationException>(() => store.Load(), "tampered encrypted data rejected");
            store.Delete();
            Check(!File.Exists(path), "unchecking storage removes encrypted profile");
            store.Delete();
        }
        finally { Directory.Delete(directory, true); }
    }
    sealed class Handler : HttpMessageHandler
    {
        readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback;
        public int Requests;
        public Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) => this.callback = callback;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;
            return callback(request, ct);
        }
    }
    sealed class MemoryStore : ICredentialStore
    {
        public int Saves, Deletes;
        public bool FailSave;
        public ConnectionSettings Value;
        public bool CanPersist => true;
        public ConnectionSettings Load() => Value?.Copy();
        public void Save(ConnectionSettings settings)
        {
            if (FailSave) throw new IOException("Simulated storage failure");
            Saves++; Value = settings.Copy();
        }
        public void Delete() { Deletes++; Value = null; }
    }
}
