using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ShopifyAdministration.Core
{
    // Framework-independent lifecycle shared by WinForms and future Avalonia/WinUI frontends.
    public sealed class LoginSession : IDisposable
    {
        private readonly ICredentialStore store;
        private readonly Func<HttpMessageHandler> transportFactory;
        private ConnectionSettings last;
        public ShopifyApiClient ActiveClient { get; private set; }
        public string ShopName { get; private set; } = "";
        public ConnectionSettings LastConnection => last?.Copy();
        public LoginSession(ICredentialStore store, Func<HttpMessageHandler> transportFactory = null)
        {
            this.store = store;
            this.transportFactory = transportFactory;
        }
        public ConnectionSettings LoadRememberedConnection()
        {
            last = store.Load();
            return LastConnection;
        }
        public async Task LoginAsync(ConnectionSettings settings, bool remember, CancellationToken cancellationToken = default)
        {
            var normalized = settings.Normalize();
            var next = new ShopifyApiClient(normalized, transportFactory?.Invoke());
            try
            {
                var name = await next.ValidateLoginAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (remember) store.Save(normalized);
                else store.Delete();
                Logout();
                last = normalized.Copy();
                ShopName = name;
                ActiveClient = next;
            }
            catch { next.Dispose(); throw; }
        }
        public void Logout()
        {
            var old = ActiveClient;
            ActiveClient = null;
            ShopName = "";
            old?.Dispose();
            // Keep last successful values only in memory for the prefilled form.
        }
        public void Dispose() { Logout(); last = null; }
    }
}
