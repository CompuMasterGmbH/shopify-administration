using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ShopifyAdministration.Core
{
    /// <summary>Owns editable login state independently of the desktop toolkit.</summary>
    public sealed class LoginPresenter : INotifyPropertyChanged, IDisposable
    {
        private readonly LoginSession session;
        private CancellationTokenSource pending;
        private string url = "", key = "", password = "", message = "";
        private bool remember, visible, busy;
        /// <summary>Initializes an opt-in login form with a copy of the last profile.</summary>
        public LoginPresenter(LoginSession session, bool canPersist)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            CanPersist = canPersist;
            Prefill();
        }
        /// <summary>Notifies views when editable or operation state changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>Gets whether encrypted persistence is available.</summary>
        public bool CanPersist { get; }
        /// <summary>Gets or sets the operator-supplied API URL.</summary>
        public string ApiUrl { get => url; set => Set(ref url, value); }
        /// <summary>Gets or sets the optional Basic authentication key.</summary>
        public string ApiKey { get => key; set => Set(ref key, value); }
        /// <summary>Gets or sets the secret; never log this property.</summary>
        public string Password { get => password; set => Set(ref password, value); }
        /// <summary>Gets or sets explicit consent to encrypted persistence.</summary>
        public bool Remember { get => remember; set => Set(ref remember, value && CanPersist); }
        /// <summary>Gets or sets explicit password visibility.</summary>
        public bool PasswordVisible { get => visible; set => Set(ref visible, value); }
        /// <summary>Gets whether authentication is in progress.</summary>
        public bool IsBusy { get => busy; private set => Set(ref busy, value); }
        /// <summary>Gets a safe operator-facing status message.</summary>
        public string Message { get => message; private set => Set(ref message, value); }
        /// <summary>Resets consent and visibility and prefills the last successful profile.</summary>
        public void Prefill()
        {
            var profile = session.LastConnection;
            ApiUrl = profile?.ApiBaseUrl ?? ""; ApiKey = profile?.ApiKey ?? ""; Password = profile?.Password ?? "";
            Remember = false; PasswordVisible = false;
            Message = CanPersist ? "" : "Kein sicherer Speicher verfügbar. Anmeldung ohne Speichern möglich.";
        }
        /// <summary>Loads and validates a remembered profile without logging its contents.</summary>
        public async Task<bool> TryAutomaticLoginAsync(CancellationToken cancellationToken = default)
        {
            if (IsBusy) return false;
            try
            {
                var saved = session.LoadRememberedConnection();
                Prefill();
                if (saved == null) return false;
                return await AuthenticateAsync(saved, true, cancellationToken);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                Message = "Gespeicherte Anmeldung nicht verfügbar. Bitte erneut anmelden.";
                return false;
            }
        }
        /// <summary>Authenticates the edited profile; cancellation never changes persistence.</summary>
        public Task<bool> SignInAsync(CancellationToken cancellationToken = default) =>
            IsBusy ? Task.FromResult(false) : AuthenticateAsync(new ConnectionSettings
            { ApiBaseUrl = ApiUrl, ApiKey = ApiKey, Password = Password }, Remember, cancellationToken);
        private async Task<bool> AuthenticateAsync(ConnectionSettings settings, bool persist, CancellationToken token)
        {
            IsBusy = true; Message = "Anmeldung wird geprüft …";
            var operation = CancellationTokenSource.CreateLinkedTokenSource(token);
            pending = operation;
            try
            {
                await session.LoginAsync(settings, persist, operation.Token);
                Message = "Angemeldet";
                return true;
            }
            catch (OperationCanceledException) { Message = "Anmeldung abgebrochen oder Zeitüberschreitung."; return false; }
            catch (Exception ex)
            {
                Message = ex is ArgumentException || ex is InvalidOperationException ? ex.Message : "Anmeldung fehlgeschlagen.";
                return false;
            }
            finally { pending = null; operation.Dispose(); IsBusy = false; }
        }
        /// <summary>Cancels pending authentication without changing the remembered profile.</summary>
        public void Cancel() => pending?.Cancel();
        /// <inheritdoc />
        public void Dispose() { Cancel(); Password = ""; }
        private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return;
            field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
