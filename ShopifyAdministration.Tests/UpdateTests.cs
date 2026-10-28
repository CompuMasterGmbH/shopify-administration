using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ShopifyAdministration.Core.Updates;

namespace ShopifyAdministration.Tests;

[TestFixture]
public sealed class UpdateTests
{
    [TestCase("1.2", "v1.2.0.0", 0)]
    [TestCase("v1.2.3+build.one", "1.2.3+other", 0)]
    [TestCase("2026.10.29", "2026.10.28.99", 1)]
    [TestCase("1.2.3", "1.2.3-rc.100", 1)]
    [TestCase("1.2.3-rc.10", "1.2.3-rc.2", 1)]
    [TestCase("1.2.3-1", "1.2.3-alpha", -1)]
    [TestCase("1.2.3-rc.02", "1.2.3-rc.2", 0)]
    [TestCase("1.2.3-alpha", "1.2.3-alpha.1", -1)]
    [TestCase("1.2.3-999999999999999999999", "1.2.3-1000000000000000000000", -1)]
    public void VersionsHaveDeterministicComponentAndPrereleaseOrdering(string left, string right, int sign)
    {
        Assert.That(UpdateVersion.TryParse(left, out var parsedLeft), Is.True);
        Assert.That(UpdateVersion.TryParse(right, out var parsedRight), Is.True);
        Assert.That(Math.Sign(parsedLeft.CompareTo(parsedRight)), Is.EqualTo(sign));
    }
    [TestCase(null)] [TestCase("")] [TestCase("1")] [TestCase("1.2.")] [TestCase("future")]
    [TestCase("1.2.3.4.5")] [TestCase("1.2.3-..bad")] [TestCase("999999999999999999999.0")]
    public void MalformedVersionsNeverFallBackToLexicalComparison(string value) => Assert.That(UpdateVersion.TryParse(value, out _), Is.False);

    [Test]
    public async Task SelectsOnlyTheNewestQualifiedMatchingRidChannelAndDeployment()
    {
        var metadata = Feed(Entry("2026.10.29"), Entry("2026.10.30"), Entry("2026.10.31", rid: "linux-x64"),
            Entry("2026.11.1-preview.1", channel: "preview"), Entry("2026.12.1", approved: false));
        using var checker = Checker(metadata);
        var result = await checker.CheckAsync(Profile());
        Assert.That(result.Status, Is.EqualTo(UpdateStatus.Available));
        Assert.That(result.Version, Is.EqualTo("2026.10.30"));
        Assert.That(result.Destination.AbsoluteUri, Does.EndWith("2026.10.30-win-x64.zip"));
    }

    [TestCase("2026.10.27")]
    [TestCase("v2026.10.28.0")]
    public async Task NeverOffersADowngradeOrEquivalentVersion(string offered)
    {
        using var checker = Checker(Feed(Entry(offered)));
        Assert.That((await checker.CheckAsync(Profile())).Status, Is.EqualTo(UpdateStatus.Current));
    }

    [TestCase("https://evil.test/app.zip")]
    [TestCase("http://github.com/CompuMasterGmbH/shopify-administration/releases/download/v1/app.zip")]
    [TestCase("https://user@github.com/CompuMasterGmbH/shopify-administration/releases/download/v1/app.zip")]
    [TestCase("https://github.com/Other/repo/releases/download/v1/app.zip")]
    [TestCase("https://github.com/CompuMasterGmbH/shopify-administration/releases/download/%2e%2e/app.zip")]
    [TestCase("https://github.com/CompuMasterGmbH/shopify-administration/releases/download/%252e%252e/shopify-administration-2026.10.29-win-x64.zip")]
    [TestCase("https://github.com/CompuMasterGmbH/shopify-administration/releases/download/v1/app.zip?token=unittests")]
    [TestCase("https://github.com/CompuMasterGmbH/shopify-administration/releases/download/v1/app.zip#other")]
    public async Task UnsafeMetadataDestinationsAreRejected(string url)
    {
        var entry = Entry("2026.10.29"); entry["url"] = url;
        using var checker = Checker(Feed(entry));
        Assert.That((await checker.CheckAsync(Profile())).Status, Is.EqualTo(UpdateStatus.Error));
    }

    [Test]
    public async Task ACorrectHostCannotSmuggleAnotherArchitectureAsset()
    {
        var entry = Entry("2026.10.29"); entry["url"] = (string)Entry("2026.10.29", rid: "linux-x64")["url"];
        using var checker = Checker(Feed(entry));
        Assert.That((await checker.CheckAsync(Profile())).Status, Is.EqualTo(UpdateStatus.Error));
    }

    [Test]
    public async Task ClickOnceRequiresSignedMetadataAndTheExactChannelManifest()
    {
        var entry = Entry("2026.10.29"); entry["deploymentType"] = "clickonce";
        entry["url"] = "https://compumastergmbh.github.io/shopify-administration/clickonce/stable/ShopifyAdministration.application";
        entry["signed"] = false;
        using (var checker = Checker(Feed(entry))) Assert.That((await checker.CheckAsync(Profile(deployment: DeploymentKind.ClickOnce))).Status, Is.EqualTo(UpdateStatus.Current));
        entry["signed"] = true;
        using (var checker = Checker(Feed(entry))) Assert.That((await checker.CheckAsync(Profile(deployment: DeploymentKind.ClickOnce))).Status, Is.EqualTo(UpdateStatus.Available));
        entry["url"] = "https://compumastergmbh.github.io/shopify-administration/clickonce/preview/ShopifyAdministration.application";
        using (var checker = Checker(Feed(entry))) Assert.That((await checker.CheckAsync(Profile(deployment: DeploymentKind.ClickOnce))).Status, Is.EqualTo(UpdateStatus.Error));
    }

    [TestCase(UpdateChannel.Development, false, DeploymentKind.Portable, UpdateStatus.Disabled)]
    [TestCase(UpdateChannel.Candidate, true, DeploymentKind.Portable, UpdateStatus.Disabled)]
    [TestCase(UpdateChannel.Stable, false, DeploymentKind.Portable, UpdateStatus.Disabled)]
    [TestCase(UpdateChannel.Stable, true, DeploymentKind.PackageManager, UpdateStatus.Managed)]
    public async Task UnqualifiedAndManagerOwnedInstallationsNeverContactTheFeed(UpdateChannel channel, bool qualified, DeploymentKind deployment, UpdateStatus expected)
    {
        var handler = new StubHandler((_, _) => throw new AssertionException("No network expected"));
        using var checker = new UpdateChecker(handler);
        var result = await checker.CheckAsync(new InstallationProfile("2026.10.28", "win-x64", channel, deployment, qualified));
        Assert.That(result.Status, Is.EqualTo(expected)); Assert.That(handler.Requests, Is.Zero);
    }

    [Test]
    public async Task UpdatesUseASeparateCredentialFreeRequestAndRejectRedirects()
    {
        using var checker = new UpdateChecker(new StubHandler((request, _) =>
        {
            Assert.That(request.RequestUri, Is.EqualTo(UpdateChecker.Feed));
            Assert.That(request.Headers.Authorization, Is.Null);
            Assert.That(request.Headers.Contains("X-Shopify-Access-Token"), Is.False);
            Assert.That(request.Headers.Contains("Cookie"), Is.False);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://evil.test/feed") } });
        }));
        Assert.That((await checker.CheckAsync(Profile())).Status, Is.EqualTo(UpdateStatus.Error));
    }

    [TestCase("[]")] [TestCase("{}")] [TestCase("not JSON")]
    [TestCase("{\"schemaVersion\":\"evil\",\"appId\":\"shopify-administration\",\"releases\":[]}")]
    [TestCase("{\"schemaVersion\":1.5,\"appId\":\"shopify-administration\",\"releases\":[]}")]
    [TestCase("{\"schemaVersion\":1,\"appId\":\"other-app\",\"releases\":[]}")]
    public async Task MalformedMetadataCannotCrashTheApplication(string json)
    {
        using var checker = new UpdateChecker(new StubHandler((_, _) => Task.FromResult(RetrievalTests.Json(json))));
        Assert.That((await checker.CheckAsync(Profile())).Status, Is.EqualTo(UpdateStatus.Error));
    }

    [Test]
    public async Task OversizedMetadataIsRejected()
    {
        using var checker = new UpdateChecker(new StubHandler((_, _) => Task.FromResult(RetrievalTests.Json(new string(' ', 524289)))));
        Assert.That((await checker.CheckAsync(Profile())).Status, Is.EqualTo(UpdateStatus.Error));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TimeoutAndCallerCancellationHaveDistinctSafeBehavior(bool stallBody)
    {
        using var checker = new UpdateChecker(new StubHandler(async (_, token) =>
        {
            if (!stallBody) await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StalledBody()) };
        }), TimeSpan.FromMilliseconds(50));
        using var cleanup = new CancellationTokenSource();
        try
        {
            Assert.That((await checker.CheckAsync(Profile(), cleanup.Token).WaitAsync(TimeSpan.FromSeconds(2))).Message, Does.Contain("Zeitüberschreitung"));
        }
        finally { cleanup.Cancel(); }
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(async () => { await checker.CheckAsync(Profile(), cancellation.Token); });
    }

    [Test]
    public async Task CallerCanCancelWhileTheMetadataBodyIsStalled()
    {
        using var body = new StalledBody();
        using var checker = new UpdateChecker(new StubHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) })));
        using var cancellation = new CancellationTokenSource();
        var check = checker.CheckAsync(Profile(), cancellation.Token);
        try { await body.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { cancellation.Cancel(); }
        await Assert.CatchAsync<OperationCanceledException>(async () => { await check.WaitAsync(TimeSpan.FromSeconds(2)); });
    }

    private sealed class StalledBody : MemoryStream
    {
        public TaskCompletionSource<bool> ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadStarted.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }
    }

    [Test]
    public async Task AutomaticCheckRunsOnlyOnceAndManualCheckIgnoresPersistedSkip()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(RetrievalTests.Json(Feed(Entry("2026.10.29")).ToString())));
        using var checker = new UpdateChecker(handler);
        var preferences = new MemoryUpdatePreferences();
        var session = new UpdateSession(checker, Profile(), preferences);
        var result = await session.CheckAsync(true); await session.SkipAsync(result);
        Assert.That((await session.CheckAsync(false)).Status, Is.EqualTo(UpdateStatus.Skipped));
        Assert.That(await session.CheckAsync(false), Is.Null);
        Assert.That((await session.CheckAsync(true)).Status, Is.EqualTo(UpdateStatus.Available));
        Assert.That(handler.Requests, Is.EqualTo(3));
        Assert.That(preferences.Scope, Is.EqualTo(Profile().Scope));
    }

    [Test]
    public async Task OfflineErrorDoesNotEchoExceptionDetails()
    {
        using var checker = new UpdateChecker(new StubHandler((_, _) => throw new HttpRequestException("unittests_private_error")));
        var result = await checker.CheckAsync(Profile());
        Assert.That(result.Status, Is.EqualTo(UpdateStatus.Error));
        Assert.That(result.Message, Does.Not.Contain("unittests_private_error"));
    }

    internal static InstallationProfile Profile(DeploymentKind deployment = DeploymentKind.Portable) => new("2026.10.28", "win-x64", UpdateChannel.Stable, deployment, true);
    internal static JObject Feed(params JObject[] entries) => new() { ["schemaVersion"] = 1, ["appId"] = InstallationProfile.AppId, ["releases"] = new JArray(entries) };
    internal static JObject Entry(string version, string rid = "win-x64", string channel = "stable", bool approved = true) => new()
    {
        ["version"] = version, ["rid"] = rid, ["channel"] = channel, ["deploymentType"] = "portable", ["approved"] = approved,
        ["sha256"] = new string('a', 64), ["url"] = "https://github.com/CompuMasterGmbH/shopify-administration/releases/download/v" + version.TrimStart('v', 'V') +
            "/shopify-administration-" + version.TrimStart('v', 'V') + "-" + rid + (rid == "linux-x64" ? ".tar.gz" : ".zip")
    };
    private static UpdateChecker Checker(JObject metadata) => new(new StubHandler((_, _) => Task.FromResult(RetrievalTests.Json(metadata.ToString()))));
    private sealed class MemoryUpdatePreferences : IUpdatePreferences
    {
        public string Scope; private string skipped;
        public Task<string> GetSkippedAsync(string scope, CancellationToken token = default) => Task.FromResult(scope == Scope ? skipped : null);
        public Task SetSkippedAsync(string scope, string version, CancellationToken token = default) { Scope = scope; skipped = version; return Task.CompletedTask; }
    }
}
