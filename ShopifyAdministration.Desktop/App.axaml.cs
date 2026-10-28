using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShopifyAdministration.Core;
using ShopifyAdministration.Platform;

namespace ShopifyAdministration.Desktop;

/// <summary>Composes native desktop services only when a desktop lifetime starts.</summary>
public sealed partial class App : Application
{
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // A test-owned startup mode never opens the operational credential store or network.
            var startupSmoke = desktop.Args?.Contains("--startup-smoke") == true;
            var demo = startupSmoke || desktop.Args?.Contains("--offline-smoke") == true;
            var store = demo ? new SessionOnlyCredentialStore() : CredentialStoreFactory.Create();
            desktop.MainWindow = new MainWindow(new LoginSession(store), store.CanPersist, demo);
            if (startupSmoke)
                desktop.MainWindow.Opened += async (_, _) =>
                {
                    await Task.Delay(500);
                    desktop.MainWindow.Close();
                };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
