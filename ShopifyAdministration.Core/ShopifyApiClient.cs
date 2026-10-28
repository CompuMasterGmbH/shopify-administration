using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ShopifyAdministration.Core
{
    public sealed class ShopifyApiClient : IDisposable
    {
        private readonly ConnectionSettings settings;
        private readonly HttpClient client;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly Func<TimeSpan, CancellationToken, Task> delay;
        private bool disposed;
        public string BaseUrl => settings.ApiBaseUrl;
        public ShopifyApiClient(ConnectionSettings configuration, HttpMessageHandler handler = null,
            Func<TimeSpan, CancellationToken, Task> retryDelay = null)
        {
            settings = configuration.Normalize();
            delay = retryDelay ?? ((duration, token) => Task.Delay(duration, token));
            client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false }, true)
            { Timeout = TimeSpan.FromSeconds(30) };
        }
        public Uri ResolveRequestUri(string resource)
        {
            var root = new Uri(BaseUrl);
            var target = Uri.TryCreate(resource, UriKind.Absolute, out var absolute) ? absolute : new Uri(root, resource);
            if (target.Scheme != Uri.UriSchemeHttps || target.Port != root.Port ||
                !string.Equals(target.IdnHost, root.IdnHost, StringComparison.OrdinalIgnoreCase) ||
                target.UserInfo != "" || target.Fragment != "" ||
                !target.AbsolutePath.StartsWith(root.AbsolutePath, StringComparison.Ordinal))
                throw new InvalidOperationException("Die Anfrage liegt außerhalb der angemeldeten Shopify-API. Es wurden keine Zugangsdaten gesendet.");
            return target;
        }
        public Task<HttpResponseMessage> GetAsync(string resource, CancellationToken cancellationToken = default) =>
            SendReadAsync(HttpMethod.Get, resource, null, cancellationToken);

        private async Task<HttpResponseMessage> SendReadAsync(HttpMethod method, string resource, string body, CancellationToken cancellationToken)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ShopifyApiClient));
            var target = ResolveRequestUri(resource);
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
            {
              for (var attempt = 0; ; attempt++)
              using (var request = new HttpRequestMessage(method, target))
              {
                if (body != null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                if (settings.ApiKey.Length == 0)
                    request.Headers.Add("X-Shopify-Access-Token", settings.Password);
                else
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.ApiKey + ":" + settings.Password)));
                try
                {
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, linked.Token).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        var status = response.StatusCode;
                        var retryAfter = response.Headers.RetryAfter;
                        var wait = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromMilliseconds(250 * (attempt + 1));
                        if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                        var retryable = status == (HttpStatusCode)429 || status == HttpStatusCode.InternalServerError || status == HttpStatusCode.BadGateway ||
                            status == HttpStatusCode.ServiceUnavailable || status == HttpStatusCode.GatewayTimeout;
                        response.Dispose();
                        if (retryable && attempt < 2 && wait <= TimeSpan.FromSeconds(30))
                        { await delay(wait, linked.Token).ConfigureAwait(false); continue; }
                        if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden)
                            throw new InvalidOperationException("Anmeldung oder Berechtigung abgelehnt (HTTP " + (int)status + "). Bitte Shop und Zugangsdaten prüfen.");
                        throw new InvalidOperationException("Shopify-Anfrage fehlgeschlagen (HTTP " + (int)status + ").");
                    }
                    return response;
                }
                catch (HttpRequestException)
                {
                    // Raw request exceptions must never expose authentication material.
                    throw new InvalidOperationException("Shopify ist nicht erreichbar. Bitte URL und Netzwerkverbindung prüfen.");
                }
              }
            }
        }
        internal async Task<JObject> ReadQueryAsync(string document, JObject variables, CancellationToken cancellationToken)
        {
            var body = new JObject { ["query"] = document, ["variables"] = variables }.ToString(Newtonsoft.Json.Formatting.None);
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
            for (var attempt = 0; ; attempt++)
            {
                using (var response = await SendReadAsync(HttpMethod.Post, "graphql.json", body, linked.Token).ConfigureAwait(false))
                {
                    JObject payload;
                    try { payload = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false)); }
                    catch (Newtonsoft.Json.JsonException) { throw new InvalidOperationException("Ungültige Shopify-API-Antwort."); }
                    linked.Token.ThrowIfCancellationRequested();
                    if (payload["errors"] != null && payload["errors"].Type != JTokenType.Null && !(payload["errors"] is JArray))
                        throw new InvalidOperationException("Ungültige Shopify-Fehlerantwort; keine Teildaten übernommen.");
                    if (payload["errors"] is JArray errors && errors.Count > 0)
                    {
                        // Never echo arbitrary server messages or accept GraphQL partial data.
                        var throttled = System.Linq.Enumerable.All(errors, error => (string)error["extensions"]?["code"] == "THROTTLED");
                        if (throttled && attempt < 2)
                        { await delay(TimeSpan.FromMilliseconds(500 * (attempt + 1)), linked.Token).ConfigureAwait(false); continue; }
                        throw new InvalidOperationException("GraphQL-Abruf abgelehnt. API-Version und Leseberechtigungen prüfen; es werden keine Teildaten übernommen.");
                    }
                    return payload["data"] as JObject ?? throw new InvalidOperationException("Shopify hat keine vollständigen Daten geliefert.");
                }
            }
        }
        public async Task<string> ValidateLoginAsync(CancellationToken cancellationToken = default)
        {
            using (var response = await GetAsync("shop.json?fields=name,myshopify_domain", cancellationToken).ConfigureAwait(false))
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    var name = (string)JObject.Parse(json)["shop"]?["name"];
                    if (string.IsNullOrWhiteSpace(name)) throw new Newtonsoft.Json.JsonException();
                    return name;
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    throw new InvalidOperationException("Die Adresse hat keine gültige Shopify-Shop-Antwort geliefert.");
                }
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            client.Dispose();
            // In-flight requests may still use the token; do not dispose their shared source.
        }
    }
}
