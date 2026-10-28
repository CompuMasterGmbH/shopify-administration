using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShopifyAdministration.Core;
using TMM_Shopify_Administration;

static class Program
{
    static int assertions;
    static void Check(bool condition, string label)
    {
        assertions++;
        if (!condition) throw new Exception("FAILED: " + label);
    }
    static T Control<T>(Form form, string name) where T : System.Windows.Forms.Control =>
        (T)form.Controls.Find(name, true)[0];
    static ToolStripMenuItem Menu(Form form, string name)
    {
        foreach (System.Windows.Forms.Control control in form.Controls)
            if (control is MenuStrip strip)
                foreach (ToolStripMenuItem top in strip.Items)
                {
                    if (top.Name == name) return top;
                    foreach (ToolStripItem child in top.DropDownItems)
                        if (child.Name == name) return (ToolStripMenuItem)child;
                }
        throw new Exception("Menu missing: " + name);
    }
    static ConnectionSettings Dummy() => new ConnectionSettings
    {
        ApiBaseUrl = "https://test-shop.myshopify.com/admin/api/2023-01/",
        ApiKey = "dummy-user", Password = "dummy-password"
    };
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        var store = new FakeStore();
        using var session = new LoginSession(store, () => new Transport());
        using (var form = new LoginForm(session, true, Dummy()))
        {
            form.Show();
            Application.DoEvents();
            var password = Control<TextBox>(form, "PasswordTextBox");
            var remember = Control<CheckBox>(form, "RememberCheckBox");
            Check(!remember.Checked, "remember defaults OFF even with prefilled values");
            Check(password.UseSystemPasswordChar, "password hidden initially");
            Check(Control<TextBox>(form, "ApiUrlTextBox").Text == Dummy().ApiBaseUrl, "URL prefilled");
            Check(Control<TextBox>(form, "ApiKeyTextBox").Text == Dummy().ApiKey, "username prefilled");
            Check(password.Text == Dummy().Password, "password prefilled");
            var toggle = Control<Button>(form, "ShowPasswordButton");
            toggle.PerformClick();
            Check(!password.UseSystemPasswordChar && toggle.Text == "Verbergen", "show password toggle");
            toggle.PerformClick();
            Check(password.UseSystemPasswordChar && toggle.Text == "Anzeigen", "hide password toggle");
            Check(password.Bounds.Width > 150 && remember.Bounds.Right <= form.ClientSize.Width, "dialog controls fit");
            if (args.Length > 0)
            {
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(Path.GetFullPath(args[0]));
            }
            Control<Button>(form, "CancelLoginButton").PerformClick();
            Check(store.Saves == 0 && store.Deletes == 0, "cancelling never writes/deletes storage");
        }
        using (var unsupported = new LoginForm(session, false, Dummy()))
        {
            Check(!Control<CheckBox>(unsupported, "RememberCheckBox").Enabled, "unsupported platform disables storage");
        }

        // Exercise the real MainForm lifecycle using a fake server and fake store only.
        store.Value = Dummy();
        using (var main = new MainForm(store, session))
        {
            main.Show();
            PumpUntil(() => session.ActiveClient != null);
            Check(main.Text.Contains("Test shop"), "startup auto-login validates remembered profile");
            Check(Menu(main, "ProdukteToolStripMenuItem").Enabled, "authenticated data actions enabled");
            Menu(main, "APIBrowserToolStripMenuItem").PerformClick();
            Application.DoEvents();
            Check(main.OwnedForms.Length == 1, "API browser belongs to active account window");
            var browser = main.OwnedForms[0];
            var table = new DataTable(); table.Columns.Add("Account"); table.Rows.Add("old account data");
            Control<DataGridView>(main, "DataGridView").DataSource = table;
            Exception timerFailure = null;
            using (var timer = new System.Windows.Forms.Timer { Interval = 30 })
            {
                timer.Tick += (sender, e) =>
                {
                    LoginForm dialog = null;
                    foreach (Form open in Application.OpenForms)
                        if (open is LoginForm login) { dialog = login; break; }
                    if (dialog == null) return;
                    timer.Stop();
                    try
                    {
                        Check(session.ActiveClient == null, "logout clears authentication before dialog");
                        Check(Control<DataGridView>(main, "DataGridView").DataSource == null, "logout clears previous account data");
                        Check(browser.IsDisposed, "logout closes previous account API browser");
                        Check(!Menu(main, "ProdukteToolStripMenuItem").Enabled, "logged-out actions disabled");
                        Check(Control<TextBox>(dialog, "PasswordTextBox").Text == Dummy().Password, "logout dialog retains previous values");
                        Check(!Control<CheckBox>(dialog, "RememberCheckBox").Checked, "logout form storage remains opt-in");
                        Control<TextBox>(dialog, "ApiUrlTextBox").Text = "https://another-shop.myshopify.com/admin/api/2023-01/";
                        Control<TextBox>(dialog, "ApiKeyTextBox").Text = "another-user";
                        Control<TextBox>(dialog, "PasswordTextBox").Text = "another-password";
                        Control<Button>(dialog, "SignInButton").PerformClick();
                    }
                    catch (Exception ex) { timerFailure = ex; dialog.Close(); }
                };
                timer.Start();
                Menu(main, "LoginToolStripMenuItem").PerformClick();
            }
            if (timerFailure != null) throw timerFailure;
            Check(session.ActiveClient != null && session.LastConnection.ApiBaseUrl.Contains("another-shop"), "another account can log in");
            Check(store.Value == null && store.Deletes > 0, "unchecked login removes old remembered profile");
            Check(Control<DataGridView>(main, "DataGridView").DataSource == null, "switch does not restore old shop data");
            main.Close();
            Check(session.ActiveClient == null, "closing main window disposes session");
        }
        Console.WriteLine("PASS: " + assertions + " UI assertions (fake credentials/server/store only).");
    }
    static void PumpUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            Application.DoEvents();
            if (DateTime.UtcNow > deadline) throw new TimeoutException("UI test timeout");
            Thread.Sleep(10);
        }
        Application.DoEvents();
    }
    sealed class FakeStore : ICredentialStore
    {
        public int Saves, Deletes;
        public ConnectionSettings Value;
        public bool CanPersist => true;
        public ConnectionSettings Load() => Value?.Copy();
        public void Save(ConnectionSettings profile) { Saves++; Value = profile.Copy(); }
        public void Delete() { Deletes++; Value = null; }
    }
    sealed class Transport : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var json = request.RequestUri.AbsolutePath.EndsWith("shop.json")
                ? "{\"shop\":{\"name\":\"Test shop\"}}" : "{\"products\":[]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
