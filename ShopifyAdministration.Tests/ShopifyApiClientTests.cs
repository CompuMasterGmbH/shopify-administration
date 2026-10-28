using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Tests;

[TestFixture]
public sealed class ShopifyApiClientTests
{
    [Test]
    public async Task BasicAuthenticationUsesSnapshotOfConfiguredCredentials()
    {
        var settings = TestProfiles.Create();
        string authorization = null;
        Uri destination = null;
        using var api = new ShopifyApiClient(settings, new StubHandler((request, _) =>
        {
            destination = request.RequestUri;
            authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(TestProfiles.ShopResponse());
        }));
        settings.Password = "changed-after-construction";
        Assert.That(await api.ValidateLoginAsync(), Is.EqualTo("Test shop"));
        Assert.That(destination.Host, Is.EqualTo("test-shop.myshopify.com"));
        Assert.That(authorization, Is.EqualTo("Basic " + Convert.ToBase64String(
            Encoding.UTF8.GetBytes("dummy-user:dummy-password-for-tests"))));
    }

    [Test]
    public async Task TokenAuthenticationDoesNotSendBasicAuthorization()
    {
        var settings = TestProfiles.Create();
        settings.ApiKey = "";
        using var api = new ShopifyApiClient(settings, new StubHandler((request, _) =>
        {
            Assert.That(request.Headers.Authorization, Is.Null);
            Assert.That(request.Headers.GetValues("X-Shopify-Access-Token").Single(), Is.EqualTo(settings.Password));
            return Task.FromResult(TestProfiles.ShopResponse());
        }));
        await api.ValidateLoginAsync();
    }

    [Test]
    public void RelativeEndpointRemainsUnderConfiguredApiRoot()
    {
        using var api = new ShopifyApiClient(TestProfiles.Create(), new StubHandler());
        Assert.That(api.ResolveRequestUri("products.json?limit=2").AbsoluteUri,
            Is.EqualTo(TestProfiles.Create().ApiBaseUrl + "products.json?limit=2"));
    }

    [TestCase("https://evil.test/admin/api/2023-01/products.json")]
    [TestCase("//evil.test/products.json")]
    [TestCase("http://test-shop.myshopify.com/admin/api/2023-01/products.json")]
    [TestCase("../2026-10/products.json")]
    [TestCase("/admin/products.json")]
    [TestCase("https://user@test-shop.myshopify.com/admin/api/2023-01/shop.json")]
    public async Task UnsafeRequestsAreRejectedBeforeCredentialsAreSent(string address)
    {
        var handler = new StubHandler();
        using var api = new ShopifyApiClient(TestProfiles.Create(), handler);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { using var response = await api.GetAsync(address); });
        Assert.That(handler.Requests, Is.Zero);
    }

    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.Redirect)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    public async Task NonSuccessfulResponsesCannotActivateLogin(HttpStatusCode status)
    {
        using var api = new ShopifyApiClient(TestProfiles.Create(), new StubHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(status))));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await api.ValidateLoginAsync(); });
    }

    [TestCase("{}")]
    [TestCase("<html>not Shopify</html>")]
    [TestCase("{\"shop\":{\"name\":\"\"}}")]
    public async Task MalformedOrMissingShopIdentityCannotActivateLogin(string body)
    {
        using var api = new ShopifyApiClient(TestProfiles.Create(), new StubHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) })));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await api.ValidateLoginAsync(); });
    }

    [Test]
    public async Task NetworkErrorsDoNotExposeTransportExceptionDetails()
    {
        using var api = new ShopifyApiClient(TestProfiles.Create(), new StubHandler(
            (_, _) => throw new HttpRequestException("dummy-password-for-tests")));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () => { await api.ValidateLoginAsync(); });
        Assert.That(error.Message, Does.Not.Contain("dummy-password-for-tests"));
    }

    [Test]
    public async Task DisposingClientCancelsPendingRequestAndRejectsNewRequests()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new ShopifyApiClient(TestProfiles.Create(), new StubHandler(async (_, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return TestProfiles.ShopResponse();
        }));
        try
        {
            var request = api.GetAsync("products.json");
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            api.Dispose();
            await Assert.CatchAsync<OperationCanceledException>(async () => { using var response = await request; });
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => { using var response = await api.GetAsync("products.json"); });
        }
        finally { api.Dispose(); }
    }
}
