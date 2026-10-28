using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using ShopifyAdministration.Core.Updates;

namespace ShopifyAdministration.Platform;

/// <summary>Detects installation ownership without reading credentials or assuming package-manager ownership from update availability.</summary>
public static class InstallationDetector
{
    /// <summary>Reads bounded package identity and validates it against the actual binary and runtime.</summary>
    public static InstallationProfile Read(string baseDirectory, string binaryVersion)
    {
        var rid = CurrentRid;
        var fallback = new InstallationProfile(binaryVersion, rid, UpdateChannel.Development, DeploymentKind.Portable, false);
        try
        {
            if (IsPackageManagerPath(baseDirectory)) return new InstallationProfile(binaryVersion, rid, UpdateChannel.Stable, DeploymentKind.PackageManager, false);
            var marker = Path.Combine(baseDirectory, "app-info.json");
            if (!File.Exists(marker) || new FileInfo(marker).Length > 8192) return fallback;
            var metadata = JObject.Parse(File.ReadAllText(marker));
            if (metadata["schemaVersion"]?.Type != JTokenType.Integer || (int?)metadata["schemaVersion"] != 1 || (string)metadata["appId"] != InstallationProfile.AppId || (string)metadata["rid"] != rid ||
                !UpdateVersion.TryParse((string)metadata["version"], out var declared) || declared.CompareTo(fallback.Version) != 0) return fallback;
            if (!Enum.TryParse<UpdateChannel>((string)metadata["channel"], true, out var channel) ||
                !Enum.TryParse<DeploymentKind>(((string)metadata["deploymentType"])?.Replace("-", ""), true, out var deployment)) return fallback;
            if (OperatingSystem.IsWindows() && string.Equals(Environment.GetEnvironmentVariable("ClickOnce_IsNetworkDeployed"), "true", StringComparison.OrdinalIgnoreCase)) deployment = DeploymentKind.ClickOnce;
            return new InstallationProfile(binaryVersion, rid, channel, deployment, metadata["stableApproved"]?.Type == JTokenType.Boolean && (bool)metadata["stableApproved"]);
        }
        catch (Exception) { return fallback; }
    }
    /// <summary>Gets the actual process OS/architecture; unsupported combinations remain unqualified.</summary>
    public static string CurrentRid => (OperatingSystem.IsWindows() ? "win-" : OperatingSystem.IsMacOS() ? "osx-" : OperatingSystem.IsLinux() ? "linux-" : "unknown-") +
        RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
    /// <summary>Recognizes explicitly manager-owned application locations; portable installers stay separate.</summary>
    public static bool IsPackageManagerPath(string path)
    {
        var absolute = Path.GetFullPath(path).Replace('\\', '/');
        if (OperatingSystem.IsWindows()) return absolute.IndexOf("/WindowsApps/", StringComparison.OrdinalIgnoreCase) >= 0;
        if (OperatingSystem.IsMacOS()) return absolute.StartsWith("/opt/homebrew/Cellar/", StringComparison.Ordinal) || absolute.StartsWith("/opt/homebrew/Caskroom/", StringComparison.Ordinal) ||
            absolute.StartsWith("/usr/local/Cellar/", StringComparison.Ordinal) || absolute.StartsWith("/usr/local/Caskroom/", StringComparison.Ordinal);
        // Installed .deb/.rpm integrations must mark package-manager ownership in app-info.json.
        // Generic /opt and /usr/local paths are not proof of package-manager ownership.
        return false;
    }
}
