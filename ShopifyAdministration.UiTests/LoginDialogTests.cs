using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using ShopifyAdministration.Core;
using TMM_Shopify_Administration;

namespace ShopifyAdministration.UiTests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable, Category("OfflineUI")]
public sealed class LoginDialogTests
{
    private FakeCredentialStore store;
    private LoginSession session;
    private LoginForm form;

    [SetUp]
    public void OpenTestDialogWithFakeCredentials()
    {
        Application.EnableVisualStyles();
        store = new FakeCredentialStore();
        session = new LoginSession(store, () => new FakeShopifyHandler());
        form = new LoginForm(session, true, UiTestSupport.Profile());
        form.Show();
        Application.DoEvents();
    }

    [TearDown]
    public void DisposeTestDialogAndSession()
    {
        form?.Close();
        form?.Dispose();
        session?.Dispose();
        Application.DoEvents();
    }

    [Test]
    public void PrefilledDialogDoesNotOptIntoPersistence()
    {
        Assert.That(UiTestSupport.Find<CheckBox>(form, "RememberCheckBox").Checked, Is.False);
        Assert.That(UiTestSupport.Find<TextBox>(form, "ApiUrlTextBox").Text, Is.EqualTo(UiTestSupport.Profile().ApiBaseUrl));
        Assert.That(UiTestSupport.Find<TextBox>(form, "ApiKeyTextBox").Text, Is.EqualTo(UiTestSupport.Profile().ApiKey));
        Assert.That(UiTestSupport.Find<TextBox>(form, "PasswordTextBox").Text, Is.EqualTo(UiTestSupport.Profile().Password));
    }

    [Test]
    public void PasswordVisibilityCanBeShownAndHiddenExplicitly()
    {
        var password = UiTestSupport.Find<TextBox>(form, "PasswordTextBox");
        var toggle = UiTestSupport.Find<Button>(form, "ShowPasswordButton");
        Assert.That(password.UseSystemPasswordChar, Is.True);
        toggle.PerformClick();
        Assert.That(password.UseSystemPasswordChar, Is.False);
        Assert.That(toggle.Text, Is.EqualTo("Verbergen"));
        toggle.PerformClick();
        Assert.That(password.UseSystemPasswordChar, Is.True);
        Assert.That(toggle.Text, Is.EqualTo("Anzeigen"));
    }

    [Test]
    public void ControlsFitWithinTheLoginDialog()
    {
        Assert.That(UiTestSupport.Find<TextBox>(form, "PasswordTextBox").Width, Is.GreaterThan(150));
        Assert.That(UiTestSupport.Find<CheckBox>(form, "RememberCheckBox").Bounds.Right, Is.LessThanOrEqualTo(form.ClientSize.Width));
    }

    [Test]
    public void CancellingDoesNotWriteOrDeleteRememberedCredentials()
    {
        UiTestSupport.Find<Button>(form, "CancelLoginButton").PerformClick();
        Assert.That(store.Saves, Is.Zero);
        Assert.That(store.Deletes, Is.Zero);
        Assert.That(session.ActiveClient, Is.Null);
    }

    [Test]
    public void UnsupportedStorageDisablesTheRememberCheckbox()
    {
        using var unsupported = new LoginForm(session, false, UiTestSupport.Profile());
        var checkbox = UiTestSupport.Find<CheckBox>(unsupported, "RememberCheckBox");
        Assert.That(checkbox.Enabled, Is.False);
        Assert.That(checkbox.Checked, Is.False);
    }

    [Test]
    public void InvalidInputKeepsDialogOpenWithoutPersistingCredentials()
    {
        UiTestSupport.Find<TextBox>(form, "ApiUrlTextBox").Text = "http://invalid.test";
        UiTestSupport.Find<Button>(form, "SignInButton").PerformClick();
        UiTestSupport.PumpUntil(() => UiTestSupport.Find<Button>(form, "SignInButton").Enabled);
        Assert.That(form.IsDisposed, Is.False);
        Assert.That(UiTestSupport.Find<Label>(form, "LoginStatusLabel").Text, Does.Contain("HTTPS"));
        Assert.That(store.Saves, Is.Zero);
        Assert.That(session.ActiveClient, Is.Null);
    }
}
