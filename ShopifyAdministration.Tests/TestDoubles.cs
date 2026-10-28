using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Tests;

internal static class TestProfiles
{
    internal static ConnectionSettings Create(string host = "test-shop") => new()
    {
        ApiBaseUrl = $"https://{host}.myshopify.com/admin/api/2023-01/",
        ApiKey = "dummy-user",
        Password = "dummy-password-for-tests"
    };

    internal static HttpResponseMessage ShopResponse() => new(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"shop\":{\"name\":\"Test shop\"}}")
    };
}

internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback;
    internal int Requests { get; private set; }
    internal StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback = null)
        => this.callback = callback ?? ((_, _) => Task.FromResult(TestProfiles.ShopResponse()));
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Requests++;
        return callback(request, token);
    }
}

internal sealed class MemoryCredentialStore : ICredentialStore
{
    internal int Saves { get; private set; }
    internal int Deletes { get; private set; }
    internal bool FailSave { get; set; }
    internal ConnectionSettings Stored { get; private set; }
    public bool CanPersist => true;
    public ConnectionSettings Load() => Stored?.Copy();
    public void Save(ConnectionSettings settings)
    {
        if (FailSave) throw new IOException("Simulated test storage failure.");
        Saves++;
        Stored = settings.Copy();
    }
    public void Delete() { Deletes++; Stored = null; }
}
