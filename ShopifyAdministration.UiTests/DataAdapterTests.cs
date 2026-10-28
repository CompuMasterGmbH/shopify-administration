using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using ShopifyAdministration.Core;
using TMM_Shopify_Administration;

namespace ShopifyAdministration.UiTests;

[TestFixture, Category("DataAdapter")]
public sealed class DataAdapterTests
{
    [Test]
    public async Task BulkRetrievalAggregatesEveryControlledResponsePage()
    {
        var destinations = new List<Uri>();
        using var client = new ShopifyApiClient(UiTestSupport.Profile(), new FakeShopifyHandler(request =>
        {
            destinations.Add(request.RequestUri);
            bool second = request.RequestUri.Query.Contains("page_info=second");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(second
                    ? "{\"products\":[{\"id\":2,\"title\":\"Second test product\"}]}"
                    : "{\"products\":[{\"id\":1,\"title\":\"First test product\"}]}")
            };
            if (!second)
                response.Headers.Add("Link", "<https://test-shop.myshopify.com/admin/api/2023-01/products.json?page_info=second>; rel=\"next\"");
            return response;
        }));
        var result = await new BulkQueries(new ShopifyQueries(client)).ProductsListAsync();
        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(destinations, Has.Count.EqualTo(2));
        Assert.That(destinations[1].Query, Does.Contain("page_info=second"));
    }

    [Test]
    public async Task ForeignPaginationLinkIsRejectedBeforeAnotherRequest()
    {
        var handler = new FakeShopifyHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"products\":[]}") };
            response.Headers.Add("Link", "<https://evil.test/admin/api/2023-01/products.json>; rel=\"next\"");
            return response;
        });
        using var client = new ShopifyApiClient(UiTestSupport.Profile(), handler);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await new BulkQueries(new ShopifyQueries(client)).ProductsListAsync();
        });
        Assert.That(handler.Requests, Is.EqualTo(1));
    }

    [Test]
    public async Task LaterPageFailureCannotBeReturnedAsACompleteList()
    {
        var handler = new FakeShopifyHandler(request =>
        {
            if (request.RequestUri.Query.Contains("page_info=second"))
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"products\":[]}") };
            response.Headers.Add("Link", "<https://test-shop.myshopify.com/admin/api/2023-01/products.json?page_info=second>; rel=\"next\"");
            return response;
        });
        using var client = new ShopifyApiClient(UiTestSupport.Profile(), handler);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await new BulkQueries(new ShopifyQueries(client)).ProductsListAsync();
        });
        Assert.That(handler.Requests, Is.EqualTo(2));
    }
}
