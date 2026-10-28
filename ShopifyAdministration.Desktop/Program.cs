using Avalonia;
using ShopifyAdministration.Platform;

namespace ShopifyAdministration.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DesktopApplicationIdentity.Configure();
        var builder = BuildAvaloniaApp();
        if (args.Contains("--startup-smoke"))
        {
            Console.WriteLine("Native startup smoke: initializing");
            if (OperatingSystem.IsLinux()) builder = builder.With(new X11PlatformOptions { RenderingMode = new[] { X11RenderingMode.Software } });
        }
        builder.StartWithClassicDesktopLifetime(args);
        if (args.Contains("--startup-smoke")) Console.WriteLine("Native startup smoke: shutdown complete");
    }
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();
}
