using System.Reflection;
using ShopifyAdministration.Core.Updates;
using ShopifyAdministration.Platform;

namespace ShopifyAdministration.Desktop;

/// <summary>Composes updater services separately from the Shopify session and credential store.</summary>
public sealed class DesktopUpdates : IDisposable
{
    private readonly UpdateChecker checker;
    private readonly Func<Uri, bool> open;
    /// <summary>Creates a controller with injectable metadata, preferences and launch behavior for offline tests.</summary>
    public DesktopUpdates(UpdateChecker checker, InstallationProfile installation, IUpdatePreferences preferences, Func<Uri, bool> open = null)
    {
        this.checker = checker; Installation = installation; Session = new UpdateSession(checker, installation, preferences);
        this.open = open ?? (destination => UpdateLauncher.Open(destination, installation));
    }
    /// <summary>Gets the detected installation qualification.</summary>
    public InstallationProfile Installation { get; }
    /// <summary>Gets the once-per-session automatic and repeatable manual checker.</summary>
    public UpdateSession Session { get; }
    /// <summary>Creates the normal per-user updater without accessing Shopify configuration.</summary>
    public static DesktopUpdates Create()
    {
        var version = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var installation = InstallationDetector.Read(AppContext.BaseDirectory, version);
        return new DesktopUpdates(new UpdateChecker(), installation, new UpdatePreferences(UpdatePreferences.DefaultPath));
    }
    /// <summary>Revalidates an offered destination before invoking its platform handler.</summary>
    public bool Open(UpdateResult result) => result?.Status == UpdateStatus.Available &&
        UpdateChecker.TryDestination(result.Destination?.AbsoluteUri, Installation.Deployment, Installation.Channel, out var destination) && open(destination);
    /// <inheritdoc />
    public void Dispose() => checker.Dispose();
}
