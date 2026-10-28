using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ShopifyAdministration.Platform;

/// <summary>Configures the Windows taskbar identity without invoking native APIs elsewhere.</summary>
public static class DesktopApplicationIdentity
{
    /// <summary>Applies the application identity on Windows; returns false on other platforms or native failure.</summary>
    public static bool Configure()
    {
        if (!OperatingSystem.IsWindows()) return false;
        return SetCurrentProcessExplicitAppUserModelID("CompuMaster.ShopifyAdministration.Desktop") == 0;
    }

    [SupportedOSPlatform("windows")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
