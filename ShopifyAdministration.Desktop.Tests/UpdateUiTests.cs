using System.Net;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ShopifyAdministration.Core;
using ShopifyAdministration.Core.Updates;
using ShopifyAdministration.Desktop;

namespace ShopifyAdministration.Desktop.Tests;

[TestFixture]
public sealed class UpdateUiTests
{
    [AvaloniaTest]
    public async Task AutomaticFailureIsQuietButManualCheckExplainsItWithoutBlockingLogin()
    {
        using var updates = new DesktopUpdates(new UpdateChecker(new Handler((_, _) => throw new HttpRequestException("unittests_private_error"))), Profile(), new Preferences());
        var window = new MainWindow(new LoginSession(new SessionOnlyCredentialStore()), false, true, updates); window.Show();
        try
        {
            await Until(() => window.FindControl<Button>("CheckUpdates").IsEnabled);
            Assert.That(window.FindControl<Grid>("UpdateNotice").IsVisible, Is.False);
            Click(window.FindControl<Button>("CheckUpdates"));
            await Until(() => window.FindControl<Grid>("UpdateNotice").IsVisible);
            Assert.That(window.FindControl<TextBlock>("UpdateMessage").Text, Does.Not.Contain("unittests_private_error"));
            Assert.That(window.FindControl<ContentControl>("LoginHost").IsVisible, Is.True);
            Assert.That(window.FindControl<Button>("OpenUpdate").IsVisible, Is.False);
        }
        finally { window.Close(); }
    }

    [AvaloniaTest]
    public async Task OfferedUpdateSupportsRemindLaterSkipAndAnExplicitValidatedDownloadAction()
    {
        var preferences = new Preferences(); Uri opened = null;
        using var updates = new DesktopUpdates(new UpdateChecker(new Handler((_, _) => Task.FromResult(Feed()))), Profile(), preferences, url => { opened = url; return true; });
        var window = new MainWindow(new LoginSession(new SessionOnlyCredentialStore()), false, true, updates); window.Show();
        try
        {
            await Until(() => window.FindControl<Button>("OpenUpdate").IsVisible);
            Click(window.FindControl<Button>("OpenUpdate"));
            Assert.That(opened.Host, Is.EqualTo("github.com"));
            Click(window.FindControl<Button>("LaterUpdate"));
            Assert.That(window.FindControl<Grid>("UpdateNotice").IsVisible, Is.False);
            Assert.That(preferences.Skipped, Is.Null);
            Click(window.FindControl<Button>("CheckUpdates"));
            await Until(() => window.FindControl<Button>("OpenUpdate").IsVisible);
            Click(window.FindControl<Button>("SkipUpdate"));
            await Until(() => !window.FindControl<Grid>("UpdateNotice").IsVisible);
            Assert.That(preferences.Skipped, Is.EqualTo("2026.10.29.0"));
        }
        finally { window.Close(); }
    }

    [AvaloniaTest]
    public async Task PendingUpdateRequestDoesNotPreventEditingTheLoginForm()
    {
        var started = new TaskCompletionSource<bool>();
        using var updates = new DesktopUpdates(new UpdateChecker(new Handler(async (_, token) =>
        { started.SetResult(true); await Task.Delay(Timeout.Infinite, token); return Feed(); })), Profile(), new Preferences());
        var window = new MainWindow(new LoginSession(new SessionOnlyCredentialStore()), false, true, updates); window.Show();
        try
        {
            await started.Task;
            var login = (LoginView)window.FindControl<ContentControl>("LoginHost").Content;
            Assert.That(login.FindControl<TextBox>("ApiUrl").IsEnabled, Is.True);
            Assert.That(login.FindControl<Button>("SignIn").IsEnabled, Is.True);
            Assert.That(window.FindControl<Button>("CheckUpdates").IsEnabled, Is.False);
        }
        finally { window.Close(); }
    }

    private static InstallationProfile Profile() => new("2026.10.28", "win-x64", UpdateChannel.Stable, DeploymentKind.Portable, true);
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Until(Func<bool> condition)
    { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)); while (!condition()) await Task.Delay(10, timeout.Token); }
    private static HttpResponseMessage Feed() => new(HttpStatusCode.OK) { Content = new StringContent(new JObject { ["schemaVersion"] = 1, ["appId"] = InstallationProfile.AppId,
        ["releases"] = new JArray(new JObject { ["version"] = "2026.10.29", ["rid"] = "win-x64", ["channel"] = "stable", ["deploymentType"] = "portable", ["approved"] = true,
            ["sha256"] = new string('a',64), ["url"] = "https://github.com/CompuMasterGmbH/shopify-administration/releases/download/v2026.10.29/shopify-administration-2026.10.29-win-x64.zip" }) }.ToString()) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => callback(request, token); }
    private sealed class Preferences : IUpdatePreferences
    {
        public string Skipped;
        public Task<string> GetSkippedAsync(string scope, CancellationToken token = default) => Task.FromResult(Skipped);
        public Task SetSkippedAsync(string scope, string version, CancellationToken token = default) { Skipped = version; return Task.CompletedTask; }
    }
}
