using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using ShopifyAdministration.Core;
using TMM_Shopify_Administration;

namespace ShopifyAdministration.UiTests;

[TestFixture]
public sealed class CollectionCompletenessTests
{
    [Test]
    public async Task CategoryViewCombinesManualAndAutomatedCollectionsInsteadOfChannelListings()
    {
        using var client = new ShopifyApiClient(UiTestSupport.Profile(), new FakeShopifyHandler(request =>
        {
            var manual = request.RequestUri.AbsolutePath.EndsWith("custom_collections.json", StringComparison.Ordinal);
            Assert.That(request.RequestUri.AbsolutePath, Does.Not.Contain("collection_listings"));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(manual
                ? "{\"custom_collections\":[{\"id\":1,\"title\":\"Manual\"}]}" : "{\"smart_collections\":[{\"id\":2,\"title\":\"Automated\"}]}") };
        }));
        var result = await new BulkQueries(new ShopifyQueries(client)).CollectionsListAsync();
        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(result.Items[0].Title, Is.EqualTo("Manual"));
        Assert.That(result.Items[1].Title, Is.EqualTo("Automated"));
    }

    [Test]
    public async Task RepeatedRestPageCannotSilentlyDuplicateProducts()
    {
        var handler = new FakeShopifyHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"products\":[{\"id\":1}]}") };
            response.Headers.Add("Link", "<products.json?limit=250>; rel=next"); return response;
        });
        using var client = new ShopifyApiClient(UiTestSupport.Profile(), handler);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await new BulkQueries(new ShopifyQueries(client)).ProductsListAsync(); });
        Assert.That(handler.Requests, Is.EqualTo(1));
    }

    [Test]
    public async Task DuplicateRestIdsAreRejectedBeforeAListIsPresented()
    {
        using var client = new ShopifyApiClient(UiTestSupport.Profile(), new FakeShopifyHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("{\"products\":[{\"id\":1},{\"id\":1}]}") }));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await new BulkQueries(new ShopifyQueries(client)).ProductsListAsync(); });
    }
}
