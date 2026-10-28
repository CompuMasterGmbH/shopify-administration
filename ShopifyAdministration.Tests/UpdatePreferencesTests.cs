using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ShopifyAdministration.Core.Updates;
using ShopifyAdministration.Platform;

namespace ShopifyAdministration.Tests;

[TestFixture]
public sealed class UpdatePreferencesTests
{
    private string directory;
    [SetUp] public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "unittests_shopify_updates_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); }
    [TearDown] public void TearDown()
    {
        var target = Path.GetFullPath(directory);
        var parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(target).StartsWith("unittests_shopify_updates_", StringComparison.Ordinal)) throw new InvalidOperationException("Test ownership check failed");
        Directory.Delete(target, true);
    }

    [Test]
    public async Task SkipPreferencesRoundTripPerScopeWithoutTouchingAdjacentProfile()
    {
        var profile = Path.Combine(directory, "credentials.v1.bin");
        await File.WriteAllTextAsync(profile, "unittests_owned_profile_sentinel");
        var path = Path.Combine(directory, "update-preferences.json");
        var preferences = new UpdatePreferences(path);
        await preferences.SetSkippedAsync("app/stable/win-x64/portable", "v1.2.3");
        await preferences.SetSkippedAsync("app/preview/win-x64/portable", "1.3.0-preview.1");
        var restored = new UpdatePreferences(path);
        Assert.That(await restored.GetSkippedAsync("app/stable/win-x64/portable"), Is.EqualTo("1.2.3.0"));
        Assert.That(await restored.GetSkippedAsync("app/stable/linux-x64/portable"), Is.Null);
        Assert.That(await File.ReadAllTextAsync(profile), Is.EqualTo("unittests_owned_profile_sentinel"));
        Assert.That(Directory.GetFiles(directory, "*.tmp-*"), Is.Empty);
    }

    [Test]
    public async Task CancelledPreferenceWriteDoesNotOverwriteExistingState()
    {
        var preferences = new UpdatePreferences(Path.Combine(directory, "preferences.json"));
        await preferences.SetSkippedAsync("scope", "1.2.3");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(async () => await preferences.SetSkippedAsync("scope", "1.2.4", cancellation.Token));
        Assert.That(await preferences.GetSkippedAsync("scope"), Is.EqualTo("1.2.3.0"));
    }

    [Test]
    public async Task MalformedExistingPreferenceFileIsPreservedInsteadOfReset()
    {
        var path = Path.Combine(directory, "preferences.json"); await File.WriteAllTextAsync(path, "unittests_invalid_json");
        var preferences = new UpdatePreferences(path);
        await Assert.CatchAsync<Newtonsoft.Json.JsonException>(async () => await preferences.SetSkippedAsync("scope", "1.2.3"));
        Assert.That(await File.ReadAllTextAsync(path), Is.EqualTo("unittests_invalid_json"));
    }

    [Test]
    public void PackageMarkerMustMatchActualVersionAndRuntimeBeforeUpdatesAreEnabled()
    {
        var metadata = new JObject { ["schemaVersion"] = 1, ["appId"] = InstallationProfile.AppId, ["version"] = "2026.10.28", ["rid"] = InstallationDetector.CurrentRid,
            ["channel"] = "stable", ["deploymentType"] = "portable", ["stableApproved"] = true };
        var marker = Path.Combine(directory, "app-info.json"); File.WriteAllText(marker, metadata.ToString());
        Assert.That(InstallationDetector.Read(directory, "2026.10.28+unittests").CanCheck, Is.True);
        metadata["rid"] = "other-architecture"; File.WriteAllText(marker, metadata.ToString());
        Assert.That(InstallationDetector.Read(directory, "2026.10.28").CanCheck, Is.False);
        metadata["rid"] = InstallationDetector.CurrentRid; metadata["version"] = "2026.10.29"; File.WriteAllText(marker, metadata.ToString());
        Assert.That(InstallationDetector.Read(directory, "2026.10.28").CanCheck, Is.False);
    }

    [Test]
    public void MissingOrDamagedMarkersAreSafeDevelopmentInstallations()
    {
        Assert.That(InstallationDetector.Read(directory, "2026.10.28").CanCheck, Is.False);
        File.WriteAllText(Path.Combine(directory, "app-info.json"), "unittests_invalid_marker");
        Assert.That(InstallationDetector.Read(directory, "2026.10.28").Channel, Is.EqualTo(UpdateChannel.Development));
    }

    [Test]
    public void PackageManagerMarkerDelegatesOwnershipWithoutEnablingSelfUpdates()
    {
        var metadata = new JObject { ["schemaVersion"] = 1, ["appId"] = InstallationProfile.AppId, ["version"] = "2026.10.28", ["rid"] = InstallationDetector.CurrentRid,
            ["channel"] = "stable", ["deploymentType"] = "package-manager", ["stableApproved"] = true };
        File.WriteAllText(Path.Combine(directory, "app-info.json"), metadata.ToString());
        Assert.That(InstallationDetector.Read(directory, "2026.10.28").Deployment, Is.EqualTo(DeploymentKind.PackageManager));
    }
}
