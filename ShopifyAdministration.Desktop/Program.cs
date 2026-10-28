using Avalonia;
using ShopifyAdministration.Platform;

namespace ShopifyAdministration.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DesktopApplicationIdentity.Configure();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();
}
