using System;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using NUnit.Framework;
using ShopifyAdministration.Platform;

namespace ShopifyAdministration.Tests;

[TestFixture, Platform("Win"), Category("WindowsStorage")]
[SupportedOSPlatform("windows")]
public sealed class WindowsCredentialStoreTests
{
    private string directory;
    private string path;
    private WindowsCredentialStore store;

    [SetUp]
    public void CreateUniqueTestOwnedStore()
    {
        directory = Path.Combine(Path.GetTempPath(), "unittests_shopify_credentials_" + Guid.NewGuid().ToString("N"));
        Assert.That(Directory.Exists(directory), Is.False);
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "credentials.v1.bin");
        store = new WindowsCredentialStore(path);
    }

    [TearDown]
    public void RemoveOnlyThisTestsDirectory()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void NewStoreIsSupportedAndInitiallyAbsent()
    {
        Assert.That(store.CanPersist, Is.True);
        Assert.That(store.Load(), Is.Null);
    }

    [Test]
    public void SaveEncryptsTheEntireProfileAndRoundTripsForCurrentUser()
    {
        var profile = TestProfiles.Create();
        store.Save(profile);
        var bytes = Encoding.UTF8.GetString(File.ReadAllBytes(path));
        Assert.That(bytes, Does.Not.Contain(profile.Password));
        Assert.That(bytes, Does.Not.Contain(profile.ApiKey));
        Assert.That(bytes, Does.Not.Contain(".myshopify.com"));
        var restored = store.Load();
        Assert.That(restored.Password, Is.EqualTo(profile.Password));
        Assert.That(restored.ApiKey, Is.EqualTo(profile.ApiKey));
        Assert.That(restored.ApiBaseUrl, Is.EqualTo(profile.ApiBaseUrl));
    }

    [Test]
    public void ReplacingProfileLeavesNoTemporaryOrPlaintextFiles()
    {
        store.Save(TestProfiles.Create());
        store.Save(TestProfiles.Create("second-shop"));
        Assert.That(store.Load().ApiBaseUrl, Does.Contain("second-shop"));
        Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
    }

    [Test]
    public void TamperingIsRejectedWithoutOverwritingDamagedProfile()
    {
        store.Save(TestProfiles.Create());
        var bytes = File.ReadAllBytes(path);
        bytes[^1] ^= 1;
        File.WriteAllBytes(path, bytes);
        Assert.Throws<InvalidOperationException>(() => store.Load());
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
    }

    [Test]
    public void UnknownStoreFormatIsRejected()
    {
        File.WriteAllText(path, "unknown profile format");
        Assert.Throws<InvalidOperationException>(() => store.Load());
    }

    [Test]
    public void DeleteRemovesRememberedProfileAndIsRepeatable()
    {
        store.Save(TestProfiles.Create());
        store.Delete();
        Assert.That(File.Exists(path), Is.False);
        Assert.DoesNotThrow(() => store.Delete());
    }
}
