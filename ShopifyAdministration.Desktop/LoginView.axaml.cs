using Avalonia.Controls;
using Avalonia.Interactivity;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Desktop;

/// <summary>Adapts the shared login presenter to Avalonia controls.</summary>
public sealed partial class LoginView : UserControl
{
    /// <summary>Occurs only after server-validated login succeeds.</summary>
    public event EventHandler SignedIn;
    /// <summary>Creates an unbound view; the owner supplies its login presenter.</summary>
    public LoginView() => InitializeComponent();
    private LoginPresenter LoginState => (LoginPresenter)DataContext;
    private void TogglePassword(object sender, RoutedEventArgs e)
    {
        LoginState.PasswordVisible = !LoginState.PasswordVisible;
        Password.PasswordChar = LoginState.PasswordVisible ? '\0' : '●';
        ShowPassword.Content = LoginState.PasswordVisible ? "Verbergen" : "Anzeigen";
    }
    private async void OnSignIn(object sender, RoutedEventArgs e)
    {
        if (LoginState.IsBusy) return;
        SetEditing(false);
        try { if (await LoginState.SignInAsync()) SignedIn?.Invoke(this, EventArgs.Empty); }
        finally { SetEditing(true); }
    }
    private void OnCancelLogin(object sender, RoutedEventArgs e) => LoginState.Cancel();
    private void SetEditing(bool enabled)
    {
        ApiUrl.IsEnabled = ApiKey.IsEnabled = Password.IsEnabled = ShowPassword.IsEnabled = enabled;
        this.FindControl<Button>("SignIn").IsEnabled = enabled;
        Remember.IsEnabled = enabled && LoginState.CanPersist;
    }
}
