using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Tests;

[TestFixture]
public sealed class LoginPresenterTests
{
    [Test]
    public async Task RememberedProfileIsValidatedBeforeAutomaticLoginAndConsentStaysOff()
    {
        var store = new MemoryCredentialStore(); store.Save(TestProfiles.Create());
        var handler = new StubHandler();
        using var session = new LoginSession(store, () => handler);
        using var presenter = new LoginPresenter(session, true);
        Assert.That(await presenter.TryAutomaticLoginAsync(), Is.True);
        Assert.That(handler.Requests, Is.EqualTo(1));
        Assert.That(presenter.Remember, Is.False);
        Assert.That(presenter.PasswordVisible, Is.False);
    }

    [Test]
    public async Task LogoutAndAccountSwitchKeepOnlyTheLastSuccessfulProfile()
    {
        var store = new MemoryCredentialStore();
        using var session = new LoginSession(store, () => new StubHandler());
        using var presenter = new LoginPresenter(session, true);
        Fill(presenter, "first-test-shop"); presenter.Remember = true;
        Assert.That(await presenter.SignInAsync(), Is.True);
        session.Logout(); presenter.Prefill();
        Assert.That(presenter.ApiUrl, Does.Contain("first-test-shop"));
        Assert.That(presenter.Remember, Is.False);
        Fill(presenter, "second-test-shop");
        Assert.That(await presenter.SignInAsync(), Is.True);
        Assert.That(store.Stored, Is.Null);
        Assert.That(session.ActiveClient.BaseUrl, Does.Contain("second-test-shop"));
    }

    [Test]
    public async Task CancellingInflightLoginKeepsStoredProfileAndRestoresEditingState()
    {
        var store = new MemoryCredentialStore(); store.Save(TestProfiles.Create());
        var started = new TaskCompletionSource<bool>();
        using var session = new LoginSession(store, () => new StubHandler(async (_, token) =>
        { started.SetResult(true); await Task.Delay(Timeout.Infinite, token); return TestProfiles.ShopResponse(); }));
        using var presenter = new LoginPresenter(session, true); Fill(presenter);
        var operation = presenter.SignInAsync(); await started.Task;
        Assert.That(presenter.IsBusy, Is.True);
        Assert.That(await presenter.SignInAsync(), Is.False);
        presenter.Cancel();
        Assert.That(await operation, Is.False);
        Assert.That(presenter.IsBusy, Is.False);
        Assert.That(session.ActiveClient, Is.Null);
        Assert.That(store.Stored, Is.Not.Null);
    }

    [Test]
    public async Task LockedStoreDoesNotActivateAnUnpersistedSessionOrDiscloseExceptions()
    {
        var store = new MemoryCredentialStore { FailSave = true };
        using var session = new LoginSession(store, () => new StubHandler());
        using var presenter = new LoginPresenter(session, true); Fill(presenter); presenter.Remember = true;
        Assert.That(await presenter.SignInAsync(), Is.False);
        Assert.That(session.ActiveClient, Is.Null);
        Assert.That(presenter.Message, Does.Not.Contain("Simulated"));
        Assert.That(presenter.IsBusy, Is.False);
    }

    [Test]
    public async Task SessionOnlyFallbackCannotBeOptedIntoPersistence()
    {
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () => new StubHandler());
        using var presenter = new LoginPresenter(session, false); Fill(presenter); presenter.Remember = true;
        Assert.That(presenter.Remember, Is.False);
        Assert.That(await presenter.SignInAsync(), Is.True);
    }

    [Test]
    public async Task DamagedStorageReturnsToEditableLogin()
    {
        using var session = new LoginSession(new DamagedStore());
        using var presenter = new LoginPresenter(session, true);
        Assert.That(await presenter.TryAutomaticLoginAsync(), Is.False);
        Assert.That(presenter.IsBusy, Is.False);
        Assert.That(presenter.Message, Does.Not.Contain("unittests_private_error"));
    }

    [Test]
    public async Task TimeoutReturnsSafeStatusAndReleasesBusyState()
    {
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () =>
            new StubHandler((_, _) => throw new TaskCanceledException("unittests_private_error")));
        using var presenter = new LoginPresenter(session, false); Fill(presenter);
        Assert.That(await presenter.SignInAsync(), Is.False);
        Assert.That(presenter.Message, Does.Contain("Zeitüberschreitung"));
        Assert.That(presenter.IsBusy, Is.False);
    }

    private static void Fill(LoginPresenter presenter, string host = "test-shop")
    { var profile = TestProfiles.Create(host); presenter.ApiUrl = profile.ApiBaseUrl; presenter.ApiKey = profile.ApiKey; presenter.Password = profile.Password; }
    private sealed class DamagedStore : ICredentialStore
    {
        public bool CanPersist => true;
        public ConnectionSettings Load() => throw new InvalidOperationException("unittests_private_error");
        public void Save(ConnectionSettings settings) => throw new NotSupportedException();
        public void Delete() => throw new NotSupportedException();
    }
}
