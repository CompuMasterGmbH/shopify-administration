using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ShopifyAdministration.Core;
using ShopifyAdministration.Platform;

namespace ShopifyAdministration.Tests;

[TestFixture]
public sealed class LoginSessionTests
{
    [Test]
    public async Task LoginWithoutRememberingRemovesAnExistingStoredProfile()
    {
        var store = new MemoryCredentialStore();
        store.Save(TestProfiles.Create("old-shop"));
        using var session = new LoginSession(store, () => new StubHandler());
        await session.LoginAsync(TestProfiles.Create(), false);
        Assert.That(store.Saves, Is.EqualTo(1));
        Assert.That(store.Deletes, Is.EqualTo(1));
        Assert.That(store.Stored, Is.Null);
        Assert.That(session.ActiveClient, Is.Not.Null);
    }

    [Test]
    public async Task LogoutDisposesOldClientButRetainsIndependentPrefillValues()
    {
        using var session = new LoginSession(new MemoryCredentialStore(), () => new StubHandler());
        await session.LoginAsync(TestProfiles.Create(), false);
        var previous = session.ActiveClient;
        session.Logout();
        Assert.That(session.ActiveClient, Is.Null);
        Assert.That(session.ShopName, Is.Empty);
        Assert.That(session.LastConnection.Password, Is.EqualTo(TestProfiles.Create().Password));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => { using var response = await previous.GetAsync("products.json"); });
        var copy = session.LastConnection;
        copy.Password = "mutated";
        Assert.That(session.LastConnection.Password, Is.Not.EqualTo(copy.Password));
    }

    [Test]
    public async Task RememberedProfileCanBeLoadedAndValidatedForAutomaticLogin()
    {
        var store = new MemoryCredentialStore();
        store.Save(TestProfiles.Create("another-shop"));
        using var session = new LoginSession(store, () => new StubHandler());
        var remembered = session.LoadRememberedConnection();
        Assert.That(session.ActiveClient, Is.Null, "Loading a disk profile must not itself authenticate.");
        await session.LoginAsync(remembered, true);
        Assert.That(session.ActiveClient.BaseUrl, Does.Contain("another-shop"));
        Assert.That(session.ShopName, Is.EqualTo("Test shop"));
        Assert.That(store.Saves, Is.EqualTo(2));
    }

    [Test]
    public async Task FailedPersistenceDoesNotReplaceAnExistingActiveSession()
    {
        var store = new MemoryCredentialStore();
        using var session = new LoginSession(store, () => new StubHandler());
        await session.LoginAsync(TestProfiles.Create(), false);
        var previous = session.ActiveClient;
        store.FailSave = true;
        await Assert.ThrowsAsync<IOException>(async () => { await session.LoginAsync(TestProfiles.Create("another-shop"), true); });
        Assert.That(session.ActiveClient, Is.SameAs(previous));
        Assert.That(session.LastConnection.ApiBaseUrl, Is.EqualTo(TestProfiles.Create().ApiBaseUrl));
    }

    [Test]
    public async Task RejectedLoginDoesNotPersistOrActivateCredentials()
    {
        var store = new MemoryCredentialStore();
        using var session = new LoginSession(store, () => new StubHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await session.LoginAsync(TestProfiles.Create(), true); });
        Assert.That(session.ActiveClient, Is.Null);
        Assert.That(store.Saves, Is.Zero);
        Assert.That(store.Deletes, Is.Zero);
    }

    [Test]
    public async Task CancelledLoginDoesNotChangeStoredCredentials()
    {
        var store = new MemoryCredentialStore();
        using var session = new LoginSession(store, () => new StubHandler());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(async () => { await session.LoginAsync(TestProfiles.Create(), true, cancellation.Token); });
        Assert.That(session.ActiveClient, Is.Null);
        Assert.That(store.Saves, Is.Zero);
    }

    [Test]
    public async Task UnsupportedStorageAllowsSessionLoginButNeverPlaintextPersistence()
    {
        var fallback = new SessionOnlyCredentialStore();
        Assert.That(fallback.CanPersist, Is.False);
        Assert.That(fallback.Load(), Is.Null);
        Assert.Throws<PlatformNotSupportedException>(() => fallback.Save(TestProfiles.Create()));
        Assert.DoesNotThrow(() => fallback.Delete());
        using var session = new LoginSession(fallback, () => new StubHandler());
        await session.LoginAsync(TestProfiles.Create(), false);
        Assert.That(session.ActiveClient, Is.Not.Null);
    }

    [Test]
    public void FactorySelectsOnlyTheCurrentPlatformsProvider()
    {
        var store = CredentialStoreFactory.Create();
        if (OperatingSystem.IsWindows())
            Assert.That(store, Is.TypeOf<WindowsCredentialStore>());
        else
            Assert.That(store, Is.TypeOf<SessionOnlyCredentialStore>());
        // Do not load, save or delete anything in the real provider.
    }

    [Test]
    public async Task DisposingSessionDropsTheInMemoryConnection()
    {
        var session = new LoginSession(new MemoryCredentialStore(), () => new StubHandler());
        await session.LoginAsync(TestProfiles.Create(), false);
        session.Dispose();
        Assert.That(session.ActiveClient, Is.Null);
        Assert.That(session.LastConnection, Is.Null);
    }
}
