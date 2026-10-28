using System.Data;
using System.Net;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using NUnit.Framework;
using OfficeOpenXml;
using ShopifyAdministration.Core;
using ShopifyAdministration.Desktop;

[assembly: AvaloniaTestApplication(typeof(ShopifyAdministration.Desktop.Tests.TestApp))]

namespace ShopifyAdministration.Desktop.Tests;

public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

[TestFixture]
public sealed class DesktopTests
{
    [AvaloniaTest]
    public void LoginStartsMaskedWithoutPersistenceAndUsesAnExplicitVisibilityButton()
    {
        using var session = new LoginSession(new SessionOnlyCredentialStore());
        using var presenter = new LoginPresenter(session, false) { Password = "unittests_secret" };
        var view = new LoginView { DataContext = presenter };
        var window = new Window { Content = view }; window.Show();
        try
        {
            var password = view.FindControl<TextBox>("Password");
            var remember = view.FindControl<CheckBox>("Remember");
            Assert.That(password.PasswordChar, Is.Not.EqualTo('\0'));
            Assert.That(remember.IsChecked, Is.False);
            Assert.That(remember.IsEnabled, Is.False);
            view.FindControl<Button>("ShowPassword").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.That(password.PasswordChar, Is.EqualTo('\0'));
            view.FindControl<Button>("ShowPassword").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.That(password.PasswordChar, Is.Not.EqualTo('\0'));
        }
        finally { window.Close(); }
    }

    [AvaloniaTest]
    public void OfflineWindowStartsAndClosesWithoutNetworkOrOperationalStorage()
    {
        var window = new MainWindow(new LoginSession(new SessionOnlyCredentialStore()), false, true);
        window.Show();
        try
        {
            Assert.That(window.FindControl<ContentControl>("LoginHost").Content, Is.TypeOf<LoginView>());
            Assert.That(window.FindControl<WrapPanel>("Actions").IsVisible, Is.False);
            Assert.That(window.FindControl<Button>("Account").IsVisible, Is.False);
        }
        finally { window.Close(); }
    }

    [AvaloniaTest]
    public async Task SignInThenLogoutClearsResultsAndPrefillsWithoutConsent()
    {
        using var session = new LoginSession(new SessionOnlyCredentialStore(), () => new ShopHandler());
        var window = new MainWindow(session, false, true); window.Show();
        try
        {
            var host = window.FindControl<ContentControl>("LoginHost");
            var view = (LoginView)host.Content;
            var presenter = (LoginPresenter)view.DataContext;
            presenter.ApiUrl = "https://unittests-shop.myshopify.com/admin/api/2026-10/";
            presenter.Password = "unittests_secret";
            view.FindControl<Button>("SignIn").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Yield();
            Assert.That(session.ActiveClient, Is.Not.Null);
            Assert.That(host.IsVisible, Is.False);
            window.FindControl<Button>("Account").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.That(session.ActiveClient, Is.Null);
            Assert.That(window.FindControl<DataGrid>("Results").ItemsSource, Is.Null);
            var next = (LoginPresenter)((LoginView)host.Content).DataContext;
            Assert.That(next.Password, Is.EqualTo("unittests_secret"));
            Assert.That(next.Remember, Is.False);
            Assert.That(next.PasswordVisible, Is.False);
        }
        finally { window.Close(); }
    }

    [Test]
    public void ExportRoundTripsUnicodeAndNumericValuesWithoutPlatformServices()
    {
        using var table = new DataTable(); table.Columns.Add("Titel"); table.Columns.Add("Preis", typeof(decimal));
        table.Rows.Add("Öl", 12.34m);
        using var package = new ExcelPackage(new MemoryStream(SpreadsheetExport.Write(table)));
        Assert.That(typeof(ExcelPackage).Assembly.GetName().Version.Major, Is.LessThan(5));
        Assert.That(package.Workbook.Worksheets["Shopify"].Cells[2, 1].Text, Is.EqualTo("Öl"));
        Assert.That(Convert.ToDecimal(package.Workbook.Worksheets["Shopify"].Cells[2, 2].Value), Is.EqualTo(12.34m));
    }

    private sealed class ShopHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"shop\":{\"name\":\"Unit test shop\"}}") });
    }
}
