using System;
using System.Diagnostics;
using System.IO;
using ShopifyAdministration.Core.Updates;

namespace ShopifyAdministration.Platform;

/// <summary>Opens only validated update destinations through an OS-appropriate external handler.</summary>
public static class UpdateLauncher
{
    /// <summary>Launches a trusted browser/ClickOnce URL; returns false when the platform has no suitable handler.</summary>
    public static bool Open(Uri destination, InstallationProfile installation)
    {
        if (destination == null || !UpdateChecker.TryDestination(destination.AbsoluteUri, installation.Deployment, installation.Channel, out _)) return false;
        if (installation.Deployment == DeploymentKind.ClickOnce && !OperatingSystem.IsWindows()) return false;
        try
        {
            if (OperatingSystem.IsWindows()) { Process.Start(new ProcessStartInfo(destination.AbsoluteUri) { UseShellExecute = true }); return true; }
            var executable = OperatingSystem.IsMacOS() ? "/usr/bin/open" : OperatingSystem.IsLinux() ? "/usr/bin/xdg-open" : "";
            if (executable == "" || !File.Exists(executable)) return false;
            var start = new ProcessStartInfo(executable) { UseShellExecute = false };
            start.ArgumentList.Add(destination.AbsoluteUri); Process.Start(start); return true;
        }
        catch (Exception) { return false; }
    }
}
