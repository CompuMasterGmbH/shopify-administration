using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ShopifyAdministration.Core;

namespace ShopifyAdministration.Tests;

[TestFixture]
public sealed class RetrievalTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(100)]
    [TestCase(201)]
    public async Task EveryProductVariantPageIsReturnedExactlyOnce(int total)
    {
        var handler = new StubHandler(async (request, _) =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            var body = JObject.Parse(await request.Content.ReadAsStringAsync());
            int offset = (int?)body["variables"]?["after"] ?? 0;
            var nodes = new JArray(Enumerable.Range(offset, Math.Min(100, total - offset)).Select(id => new JObject { ["id"] = "variant-" + id, ["product"] = new JObject { ["id"] = "product-" + id } }));
            return Page("productVariants", nodes, offset + nodes.Count < total, (offset + nodes.Count).ToString());
        });
        using var client = new ShopifyApiClient(TestProfiles.Create(), handler);
        var records = await new ShopifyDataReader(client).ReadAsync(ShopifyDataset.Products);
        Assert.That(records, Has.Count.EqualTo(total));
        Assert.That(records.Select(record => (string)record["id"]).Distinct().Count(), Is.EqualTo(total));
        Assert.That(handler.Requests, Is.EqualTo(Math.Max(1, (total + 99) / 100)));
    }

    [TestCase("{\"nodes\":[],\"pageInfo\":{\"hasNextPage\":true,\"endCursor\":\"next\"}}")]
    [TestCase("{\"nodes\":[{\"id\":\"one\"}],\"pageInfo\":{\"hasNextPage\":true,\"endCursor\":null}}")]
    [TestCase("{\"nodes\":[{\"title\":\"missing id\"}],\"pageInfo\":{\"hasNextPage\":false}}")]
    [TestCase("{\"nodes\":[{\"id\":\"one\"},{\"id\":\"one\"}],\"pageInfo\":{\"hasNextPage\":false}}")]
    [TestCase("{\"nodes\":[]}")]
    [TestCase("null")]
    public async Task MalformedConnectionsNeverBecomeSuccessfulEmptyOrPartialResults(string connection)
    {
        using var client = new ShopifyApiClient(TestProfiles.Create(), new StubHandler((_, _) => Task.FromResult(Json("{\"data\":{\"collections\":" + connection + "}}"))));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await new ShopifyDataReader(client).ReadAsync(ShopifyDataset.Collections); });
    }

    [Test]
    public async Task RepeatedCursorFailsEvenWhenRecordsHaveDifferentIds()
    {
        var count = 0;
        using var client = new ShopifyApiClient(TestProfiles.Create(), new StubHandler((_, _) =>
            Task.FromResult(Page("collections", new JArray(new JObject { ["id"] = "id-" + ++count }), true, "repeated"))));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { await new ShopifyDataReader(client).ReadAsync(ShopifyDataset.Collections); });
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task LaterPagePermissionErrorsRejectPartialGraphQlDataWithoutEchoingServerSecrets()
    {
        var count = 0;
        using var client = new ShopifyApiClient(TestProfiles.Create(), new StubHandler((_, _) => Task.FromResult(++count == 1
            ? Page("collections", new JArray(new JObject { ["id"] = "first" }), true, "second")
            : Json("{\"data\":{\"collections\":{\"nodes\":[{\"id\":\"partial\"}]}},\"errors\":[{\"message\":\"dummy-password-for-tests\",\"extensions\":{\"code\":\"ACCESS_DENIED\"}}]}"))));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () => { await new ShopifyDataReader(client).ReadAsync(ShopifyDataset.Collections); });
        Assert.That(error.Message, Does.Not.Contain("dummy-password-for-tests"));
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task TransfersIncludeAllNestedLinePagesAndRetainAnEmptyTransferHeader()
    {
        var handler = new StubHandler(async (request, _) =>
        {
            var body = JObject.Parse(await request.Content.ReadAsStringAsync());
            var variables = body["variables"];
            if (variables["id"] == null)
                return Page("inventoryTransfers", new JArray(new JObject { ["id"] = "one", ["name"] = "T1" }, new JObject { ["id"] = "two", ["name"] = "T2" }));
            var second = variables["after"]?.Type == JTokenType.String;
            var nodes = (string)variables["id"] == "two" ? new JArray() : new JArray(new JObject { ["id"] = second ? "line-2" : "line-1", ["totalQuantity"] = second ? 3 : 2 });
            var page = Page("lineItems", nodes, (string)variables["id"] == "one" && !second, "next");
            var json = JObject.Parse(await page.Content.ReadAsStringAsync());
            return Json(new JObject { ["data"] = new JObject { ["inventoryTransfer"] = json["data"] } }.ToString());
        });
        using var client = new ShopifyApiClient(TestProfiles.Create(), handler);
        var result = await new ShopifyDataReader(client).ReadAsync(ShopifyDataset.Transfers);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Where(row => row["line"] != null).Sum(row => (int)row["line"]["totalQuantity"]), Is.EqualTo(5));
        Assert.That(result.Count(row => (string)row["transfer"]["id"] == "two" && row["line"] == null), Is.EqualTo(1));
        Assert.That(handler.Requests, Is.EqualTo(4));
    }

    [TestCase(ShopifyDataset.Collections, null)]
    [TestCase(ShopifyDataset.SmartCollections, "collection_type:smart")]
    public async Task CollectionSelectionIsExplicitAndCoversTheRequestedScope(ShopifyDataset dataset, string filter)
    {
        using var client = new ShopifyApiClient(TestProfiles.Create(), new StubHandler(async (request, _) =>
        {
            var body = JObject.Parse(await request.Content.ReadAsStringAsync());
            Assert.That((string)body["variables"]["filter"], Is.EqualTo(filter));
            return Page("collections", new JArray());
        }));
        Assert.That(await new ShopifyDataReader(client).ReadAsync(dataset), Is.Empty);
    }

    [Test]
    public async Task RetryAfterIsRespectedAndReadRetriesAreBounded()
    {
        var delays = new List<TimeSpan>();
        var handler = new StubHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
            return Task.FromResult(response);
        });
        using var client = new ShopifyApiClient(TestProfiles.Create(), handler, (delay, _) => { delays.Add(delay); return Task.CompletedTask; });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { using var result = await client.GetAsync("shop.json"); });
        Assert.That(handler.Requests, Is.EqualTo(3));
        Assert.That(delays, Is.EqualTo(new[] { TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(7) }));
    }

    [Test]
    public async Task ExcessiveRetryAfterFailsWithoutRetryingBeforeTheRequestedTime()
    {
        var handler = new StubHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(5)); return Task.FromResult(response);
        });
        using var client = new ShopifyApiClient(TestProfiles.Create(), handler, (_, _) => throw new AssertionException("Must not retry early"));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { using var response = await client.GetAsync("shop.json"); });
        Assert.That(handler.Requests, Is.EqualTo(1));
    }

    [Test]
    public async Task GraphQlThrottlingRetriesTheSameReadAndAcceptsOnlyTheCompleteResult()
    {
        var count = 0; var waits = 0;
        using var client = new ShopifyApiClient(TestProfiles.Create(), new StubHandler((_, _) => Task.FromResult(++count == 1
            ? Json("{\"errors\":[{\"extensions\":{\"code\":\"THROTTLED\"}}]}") : Page("collections", new JArray()))), (_, _) => { waits++; return Task.CompletedTask; });
        Assert.That(await new ShopifyDataReader(client).ReadAsync(ShopifyDataset.Collections), Is.Empty);
        Assert.That(count, Is.EqualTo(2)); Assert.That(waits, Is.EqualTo(1));
    }

    [Test]
    public async Task LogoutCancelsRetryWaits()
    {
        var waiting = new TaskCompletionSource<bool>();
        var client = new ShopifyApiClient(TestProfiles.Create(), new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests))),
            async (_, token) => { waiting.SetResult(true); await Task.Delay(Timeout.Infinite, token); });
        var operation = client.GetAsync("products.json"); await waiting.Task;
        client.Dispose();
        await Assert.CatchAsync<OperationCanceledException>(async () => { using var result = await operation; });
    }

    [TestCase("<?page_info=next>; title=\"page\"; rel=\"previous next\"", "products.json?page_info=next")]
    [TestCase("<products.json?page_info=next>; rel=next", "products.json?page_info=next")]
    public void RestNextLinksResolveAgainstTheCurrentResource(string header, string suffix)
    {
        using var client = new ShopifyApiClient(TestProfiles.Create(), new StubHandler());
        Assert.That(RestPagination.Next(client, "products.json?limit=250", new[] { header }), Is.EqualTo(client.BaseUrl + suffix));
    }

    [TestCase("<https://evil.test/products.json>; rel=next")]
    [TestCase("broken; rel=next")]
    [TestCase("<products.json?a=1>; rel=next, <products.json?a=2>; rel=next")]
    public void UnsafeOrAmbiguousRestNextLinksFail(string header)
    {
        using var client = new ShopifyApiClient(TestProfiles.Create(), new StubHandler());
        Assert.Throws<InvalidOperationException>(() => RestPagination.Next(client, "products.json", new[] { header }));
    }

    internal static HttpResponseMessage Page(string field, JArray nodes, bool next = false, string cursor = null) => Json(new JObject
    { ["data"] = new JObject { [field] = new JObject { ["nodes"] = nodes, ["pageInfo"] = new JObject { ["hasNextPage"] = next, ["endCursor"] = cursor } } } }.ToString());
    internal static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };
}
