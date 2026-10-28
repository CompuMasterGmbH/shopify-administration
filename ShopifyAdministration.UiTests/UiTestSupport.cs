using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using ShopifyAdministration.Core;
using TMM_Shopify_Administration;

namespace ShopifyAdministration.UiTests;

internal static class UiTestSupport
{
    internal static ConnectionSettings Profile() => new()
    {
        ApiBaseUrl = "https://test-shop.myshopify.com/admin/api/2023-01/",
        ApiKey = "dummy-user", Password = "dummy-password"
    };

    internal static T Find<T>(Form form, string name) where T : Control
        => (T)form.Controls.Find(name, true).Single();

    internal static ToolStripMenuItem Menu(Form form, string name)
    {
        foreach (var strip in form.Controls.OfType<MenuStrip>())
        foreach (ToolStripMenuItem top in strip.Items)
        {
            if (top.Name == name) return top;
            foreach (ToolStripItem child in top.DropDownItems)
                if (child.Name == name) return (ToolStripMenuItem)child;
        }
        throw new InvalidOperationException("Test menu item not found: " + name);
    }

    internal static void PumpUntil(Func<bool> completed)
    {
        var watch = Stopwatch.StartNew();
        while (!completed())
        {
            Application.DoEvents();
            if (watch.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("Offline UI test timed out.");
            Thread.Sleep(10);
        }
        Application.DoEvents();
    }

    internal static void InteractWithRelogin(MainForm main, Action<LoginForm> action)
    {
        Exception failure = null;
        bool acted = false;
        var watch = Stopwatch.StartNew();
        using var timer = new System.Windows.Forms.Timer { Interval = 20 };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.OpenForms.OfType<LoginForm>().FirstOrDefault();
            if (watch.Elapsed > TimeSpan.FromSeconds(5))
            {
                failure ??= new TimeoutException("Relogin dialog did not finish.");
                dialog?.Close();
                return;
            }
            if (acted || dialog == null) return;
            acted = true;
            try { action(dialog); }
            catch (Exception error) { failure = error; dialog.Close(); }
        };
        timer.Start();
        Menu(main, "LoginToolStripMenuItem").PerformClick();
        Assert.That(failure, Is.Null);
        Assert.That(acted, Is.True);
    }
}

internal sealed class FakeCredentialStore : ICredentialStore
{
    internal int Saves { get; private set; }
    internal int Deletes { get; private set; }
    internal ConnectionSettings Stored { get; set; }
    public bool CanPersist => true;
    public ConnectionSettings Load() => Stored?.Copy();
    public void Save(ConnectionSettings profile) { Saves++; Stored = profile.Copy(); }
    public void Delete() { Deletes++; Stored = null; }
}

internal sealed class FakeShopifyHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> callback;
    internal int Requests { get; private set; }
    internal FakeShopifyHandler(Func<HttpRequestMessage, HttpResponseMessage> callback = null)
        => this.callback = callback ?? (request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(request.RequestUri.AbsolutePath.EndsWith("shop.json")
                ? "{\"shop\":{\"name\":\"Test shop\"}}" : "{\"products\":[]}")
        });
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Requests++;
        return Task.FromResult(callback(request));
    }
}
