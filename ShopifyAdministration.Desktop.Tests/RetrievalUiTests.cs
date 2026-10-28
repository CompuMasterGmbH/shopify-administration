using System.Net;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ShopifyAdministration.Core;
using ShopifyAdministration.Desktop;

namespace ShopifyAdministration.Desktop.Tests;

[TestFixture]
public sealed class RetrievalUiTests
{
    [AvaloniaTest]
    public async Task PendingReadKeepsUiResponsiveShowsWaitStateAndCanBeCancelled()
    {
        var started = new TaskCompletionSource<bool>();
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () => new Handler(async (request, token) =>
        {
            if (request.Method == HttpMethod.Get) return Shop();
            started.SetResult(true); await Task.Delay(Timeout.Infinite, token); return Page();
        }));
        var window = await SignedInWindow(session);
        try
        {
            Click(window.FindControl<Button>("Products")); await started.Task;
            Assert.That(window.FindControl<ProgressBar>("Progress").IsVisible, Is.True);
            Assert.That(window.Cursor, Is.Not.EqualTo(Cursor.Default));
            Assert.That(window.FindControl<WrapPanel>("Actions").IsEnabled, Is.False);
            Assert.That(window.FindControl<Button>("Account").IsEnabled, Is.True);
            Assert.That(window.FindControl<Button>("Export").IsEnabled, Is.False);
            Click(window.FindControl<Button>("CancelLoad"));
            await Until(() => !window.FindControl<ProgressBar>("Progress").IsVisible);
            Assert.That(window.FindControl<WrapPanel>("Actions").IsEnabled, Is.True);
            Assert.That(window.FindControl<DataGrid>("Results").ItemsSource, Is.Null);
            Assert.That(window.FindControl<Button>("Export").IsEnabled, Is.False);
        }
        finally { window.Close(); }
    }

    [AvaloniaTest]
    public async Task LogoutDuringReadClearsDataAndIgnoresTheLateResponse()
    {
        var started = new TaskCompletionSource<bool>(); var release = new TaskCompletionSource<bool>();
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () => new Handler(async (request, _) =>
        {
            if (request.Method == HttpMethod.Get) return Shop();
            started.SetResult(true); await release.Task; return Page();
        }));
        var window = await SignedInWindow(session);
        try
        {
            Click(window.FindControl<Button>("Products")); await started.Task;
            Click(window.FindControl<Button>("Account")); release.SetResult(true);
            await Until(() => !window.FindControl<ProgressBar>("Progress").IsVisible);
            Assert.That(window.FindControl<ContentControl>("LoginHost").IsVisible, Is.True);
            Assert.That(window.FindControl<DataGrid>("Results").ItemsSource, Is.Null);
            Assert.That(window.FindControl<Button>("Export").IsEnabled, Is.False);
            Assert.That(session.ActiveClient, Is.Null);
        }
        finally { release.TrySetResult(true); window.Close(); }
    }

    [AvaloniaTest]
    public async Task FailedReadRestoresCursorAndExplainsFailureWithoutExportingPartialResults()
    {
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () => new Handler((request, _) => Task.FromResult(
            request.Method == HttpMethod.Get ? Shop() : new HttpResponseMessage(HttpStatusCode.Forbidden))));
        var window = await SignedInWindow(session);
        try
        {
            Click(window.FindControl<Button>("Products"));
            await Until(() => !window.FindControl<ProgressBar>("Progress").IsVisible);
            Assert.That(window.Cursor, Is.EqualTo(Cursor.Default));
            Assert.That(window.FindControl<TextBlock>("Status").Text, Does.Contain("403"));
            Assert.That(window.FindControl<Button>("Export").IsEnabled, Is.False);
        }
        finally { window.Close(); }
    }

    [Test]
    public void TabularMappingPreservesNullsPricesParentIdsAndArrayContents()
    {
        using var table = RecordTable.Create(new[] { JObject.Parse("{\"id\":\"variant-1\",\"price\":\"12.34\",\"compareAtPrice\":null,\"product\":{\"id\":\"product-1\",\"title\":\"Öl\",\"tags\":[\"one,two\",\"three\"]}}") });
        Assert.That(table.Rows[0]["price"], Is.EqualTo(12.34m));
        Assert.That(table.Rows[0]["compareAtPrice"], Is.EqualTo(DBNull.Value));
        Assert.That(table.Rows[0]["product.id"], Is.EqualTo("product-1"));
        Assert.That(JArray.Parse((string)table.Rows[0]["product.tags"]), Has.Count.EqualTo(2));
    }

    private static async Task<MainWindow> SignedInWindow(LoginSession session)
    {
        var window = new MainWindow(session, false, true); window.Show();
        var view = (LoginView)window.FindControl<ContentControl>("LoginHost").Content;
        var login = (LoginPresenter)view.DataContext;
        login.ApiUrl = "https://unittests-shop.myshopify.com/admin/api/2026-10/"; login.Password = "unittests_secret";
        Click(view.FindControl<Button>("SignIn"));
        await Until(() => !window.FindControl<ContentControl>("LoginHost").IsVisible);
        return window;
    }
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Until(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }
    private static HttpResponseMessage Shop() => new(HttpStatusCode.OK) { Content = new StringContent("{\"shop\":{\"name\":\"Unit tests\"}}") };
    private static HttpResponseMessage Page() => new(HttpStatusCode.OK) { Content = new StringContent("{\"data\":{\"productVariants\":{\"nodes\":[{\"id\":\"one\"}],\"pageInfo\":{\"hasNextPage\":false}}}}") };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => callback(request, token);
    }
}
