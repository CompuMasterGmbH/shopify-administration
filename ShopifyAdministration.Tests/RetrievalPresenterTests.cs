using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Tests;

[TestFixture]
public sealed class RetrievalPresenterTests
{
    [Test]
    public async Task PendingLoadYieldsAndRejectsContradictoryRefreshThenCancelRestoresState()
    {
        var started = new TaskCompletionSource<bool>(); var reads = 0;
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () => new StubHandler(async (request, token) =>
        {
            if (request.Method == HttpMethod.Get) return TestProfiles.ShopResponse();
            reads++; started.SetResult(true); await Task.Delay(Timeout.Infinite, token);
            return RetrievalTests.Page("collections", new JArray());
        }));
        await session.LoginAsync(TestProfiles.Create(), false);
        using var presenter = new RetrievalPresenter(session);
        var operation = presenter.LoadAsync(ShopifyDataset.Collections); await started.Task;
        Assert.That(operation.IsCompleted, Is.False);
        Assert.That(presenter.IsLoading, Is.True); Assert.That(presenter.CanExport, Is.False);
        await presenter.LoadAsync(ShopifyDataset.Products);
        Assert.That(reads, Is.EqualTo(1));
        presenter.Cancel(); await operation;
        Assert.That(presenter.IsLoading, Is.False); Assert.That(presenter.CanExport, Is.False);
        Assert.That(presenter.Records, Is.Empty);
    }

    [Test]
    public async Task LateOldSessionCannotReplaceNewAccountResultsOrLoadingState()
    {
        var started = new TaskCompletionSource<bool>();
        var release = new TaskCompletionSource<bool>();
        var old = true;
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () =>
        {
            var isOld = old;
            return new StubHandler(async (request, _) =>
            {
                if (request.Method == HttpMethod.Get) return TestProfiles.ShopResponse();
                if (isOld) { started.SetResult(true); await release.Task; }
                return RetrievalTests.Page("collections", new JArray(new JObject { ["id"] = isOld ? "old" : "new" }));
            });
        });
        await session.LoginAsync(TestProfiles.Create(), false);
        using var presenter = new RetrievalPresenter(session);
        var oldLoad = presenter.LoadAsync(ShopifyDataset.Collections); await started.Task;
        presenter.Clear(); session.Logout(); old = false;
        await session.LoginAsync(TestProfiles.Create("new-test-shop"), false);
        await presenter.LoadAsync(ShopifyDataset.Collections);
        release.SetResult(true); await oldLoad;
        Assert.That(presenter.Records, Has.Count.EqualTo(1));
        Assert.That((string)presenter.Records[0]["id"], Is.EqualTo("new"));
        Assert.That(presenter.IsLoading, Is.False); Assert.That(presenter.CanExport, Is.True);
    }

    [Test]
    public async Task FailedRefreshRemovesOldSuccessfulDataAndCannotBeExported()
    {
        var fail = false;
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () => new StubHandler((request, _) =>
            Task.FromResult(request.Method == HttpMethod.Get ? TestProfiles.ShopResponse() : fail
                ? RetrievalTests.Json("{}") : RetrievalTests.Page("collections", new JArray(new JObject { ["id"] = "success" })))));
        await session.LoginAsync(TestProfiles.Create(), false);
        using var presenter = new RetrievalPresenter(session);
        await presenter.LoadAsync(ShopifyDataset.Collections);
        Assert.That(presenter.CanExport, Is.True);
        fail = true; await presenter.LoadAsync(ShopifyDataset.Collections);
        Assert.That(presenter.Records, Is.Empty); Assert.That(presenter.CanExport, Is.False); Assert.That(presenter.IsLoading, Is.False);
    }

    [Test]
    public async Task NetworkTimeoutRestoresStateAndKeepsNoPartialData()
    {
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () => new StubHandler((request, _) =>
            request.Method == HttpMethod.Get ? Task.FromResult(TestProfiles.ShopResponse()) : throw new TaskCanceledException()));
        await session.LoginAsync(TestProfiles.Create(), false);
        using var presenter = new RetrievalPresenter(session);
        await presenter.LoadAsync(ShopifyDataset.Collections);
        Assert.That(presenter.Status, Does.Contain("Zeitüberschreitung"));
        Assert.That(presenter.IsLoading, Is.False); Assert.That(presenter.CanExport, Is.False);
    }
}
