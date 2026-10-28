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
        private bool disposed;
        public string BaseUrl => settings.ApiBaseUrl;
        public ShopifyApiClient(ConnectionSettings configuration, HttpMessageHandler handler = null)
        {
            settings = configuration.Normalize();
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
        public async Task<HttpResponseMessage> GetAsync(string resource, CancellationToken cancellationToken = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ShopifyApiClient));
            var target = ResolveRequestUri(resource);
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
            using (var request = new HttpRequestMessage(HttpMethod.Get, target))
            {
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
                        response.Dispose();
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
