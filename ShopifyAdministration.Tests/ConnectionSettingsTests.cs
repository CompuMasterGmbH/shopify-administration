using System;
using NUnit.Framework;

namespace ShopifyAdministration.Tests;

[TestFixture]
public sealed class ConnectionSettingsTests
{
    [Test]
    public void NormalizeCanonicalizesHostWhitespaceAndTrailingSlash()
    {
        var profile = TestProfiles.Create();
        profile.ApiBaseUrl = " https://TEST-SHOP.myshopify.com/admin/api/2023-01 ";
        Assert.That(profile.Normalize().ApiBaseUrl, Is.EqualTo(TestProfiles.Create().ApiBaseUrl));
    }

    [TestCase("http://test-shop.myshopify.com/admin/api/2023-01/")]
    [TestCase("https://test-shop.myshopify.com.evil.test/admin/api/2023-01/")]
    [TestCase("https://user:password@test-shop.myshopify.com/admin/api/2023-01/")]
    [TestCase("https://test-shop.myshopify.com/admin/api/2023-01/?token=x")]
    [TestCase("https://test-shop.myshopify.com/admin/api/2023-01/#secret")]
    [TestCase("https://test-shop.myshopify.com:444/admin/api/2023-01/")]
    [TestCase("https://test-shop.myshopify.com/admin/api/2023-02/")]
    [TestCase("https://test-shop.myshopify.com/")]
    [TestCase("not-a-url")]
    public void NormalizeRejectsUnsafeOrAmbiguousAddresses(string address)
    {
        var profile = TestProfiles.Create();
        profile.ApiBaseUrl = address;
        Assert.Throws<ArgumentException>(() => profile.Normalize());
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("token\r\ninjected")]
    public void NormalizeRejectsMissingPasswordsAndHeaderInjection(string password)
    {
        var profile = TestProfiles.Create();
        profile.Password = password;
        Assert.Throws<ArgumentException>(() => profile.Normalize());
    }

    [TestCase("bad:key")]
    [TestCase("bad\r\nkey")]
    public void NormalizeRejectsInvalidBasicUsernames(string apiKey)
    {
        var profile = TestProfiles.Create();
        profile.ApiKey = apiKey;
        Assert.Throws<ArgumentException>(() => profile.Normalize());
    }

    [Test]
    public void NormalizePreservesPasswordWhitespaceAndReturnsIndependentSettings()
    {
        var source = TestProfiles.Create();
        source.Password = " password with spaces ";
        var normalized = source.Normalize();
        source.Password = "changed";
        Assert.That(normalized.Password, Is.EqualTo(" password with spaces "));
    }
}
