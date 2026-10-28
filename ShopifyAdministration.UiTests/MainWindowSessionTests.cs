using System.Data;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using ShopifyAdministration.Core;
using TMM_Shopify_Administration;

namespace ShopifyAdministration.UiTests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable, Category("OfflineUI")]
public sealed class MainWindowSessionTests
{
    private FakeCredentialStore store;
    private LoginSession session;
    private MainForm main;

    [SetUp]
    public void OpenWindowWithFakeRememberedProfile()
    {
        Application.EnableVisualStyles();
        store = new FakeCredentialStore { Stored = UiTestSupport.Profile() };
        session = new LoginSession(store, () => new FakeShopifyHandler());
        main = new MainForm(store, session);
        main.Show();
        UiTestSupport.PumpUntil(() => session.ActiveClient != null);
    }

    [TearDown]
    public void CloseOnlyTestOwnedWindows()
    {
        main?.Close();
        main?.Dispose();
        session?.Dispose();
        Application.DoEvents();
    }

    [Test]
    public void RememberedProfileIsValidatedBeforeEnablingDataActions()
    {
        Assert.That(main.Text, Does.Contain("Test shop"));
        Assert.That(UiTestSupport.Menu(main, "ProdukteToolStripMenuItem").Enabled, Is.True);
        Assert.That(store.Saves, Is.EqualTo(1));
    }

    [Test]
    public void LogoutClearsDataAndWindowsAndSupportsAnotherAccountWithoutPersistence()
    {
        UiTestSupport.Menu(main, "APIBrowserToolStripMenuItem").PerformClick();
        Application.DoEvents();
        Assert.That(main.OwnedForms, Has.Length.EqualTo(1));
        var browser = main.OwnedForms[0];
        using var table = new DataTable();
        table.Columns.Add("Account");
        table.Rows.Add("old test account");
        UiTestSupport.Find<DataGridView>(main, "DataGridView").DataSource = table;
        UiTestSupport.InteractWithRelogin(main, dialog =>
        {
            Assert.That(session.ActiveClient, Is.Null);
            Assert.That(UiTestSupport.Find<DataGridView>(main, "DataGridView").DataSource, Is.Null);
            Assert.That(browser.IsDisposed, Is.True);
            Assert.That(UiTestSupport.Menu(main, "ProdukteToolStripMenuItem").Enabled, Is.False);
            Assert.That(UiTestSupport.Find<TextBox>(dialog, "PasswordTextBox").Text, Is.EqualTo(UiTestSupport.Profile().Password));
            Assert.That(UiTestSupport.Find<CheckBox>(dialog, "RememberCheckBox").Checked, Is.False);
            UiTestSupport.Find<TextBox>(dialog, "ApiUrlTextBox").Text = "https://another-shop.myshopify.com/admin/api/2023-01/";
            UiTestSupport.Find<TextBox>(dialog, "ApiKeyTextBox").Text = "another-user";
            UiTestSupport.Find<TextBox>(dialog, "PasswordTextBox").Text = "another-password";
            UiTestSupport.Find<Button>(dialog, "SignInButton").PerformClick();
        });
        Assert.That(session.ActiveClient, Is.Not.Null);
        Assert.That(session.LastConnection.ApiBaseUrl, Does.Contain("another-shop"));
        Assert.That(store.Stored, Is.Null);
        Assert.That(store.Deletes, Is.EqualTo(1));
        Assert.That(UiTestSupport.Find<DataGridView>(main, "DataGridView").DataSource, Is.Null);
    }

    [Test]
    public void CancellingReloginLeavesWindowLoggedOutWithoutDeletingRememberedProfile()
    {
        UiTestSupport.InteractWithRelogin(main,
            dialog => UiTestSupport.Find<Button>(dialog, "CancelLoginButton").PerformClick());
        Assert.That(session.ActiveClient, Is.Null);
        Assert.That(store.Stored, Is.Not.Null);
        Assert.That(store.Deletes, Is.Zero);
        Assert.That(UiTestSupport.Menu(main, "ProdukteToolStripMenuItem").Enabled, Is.False);
    }

    [Test]
    public void ClosingMainWindowDisposesActiveSession()
    {
        main.Close();
        Assert.That(session.ActiveClient, Is.Null);
        Assert.That(session.LastConnection, Is.Null);
    }
}
