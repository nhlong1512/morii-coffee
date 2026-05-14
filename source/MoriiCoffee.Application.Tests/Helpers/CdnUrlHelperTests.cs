using FluentAssertions;
using MoriiCoffee.Application.SeedWork.Helpers;
using Xunit;

namespace MoriiCoffee.Application.Tests.Helpers;

public class CdnUrlHelperTests
{
    [Fact]
    public void Resolve_NullKey_ReturnsNull()
    {
        CdnUrlHelper.Resolve(null, "https://cdn.test").Should().BeNull();
    }

    [Fact]
    public void Resolve_EmptyKey_ReturnsNull()
    {
        CdnUrlHelper.Resolve("", "https://cdn.test").Should().BeNull();
    }

    [Fact]
    public void Resolve_KeyAlreadyAbsoluteUrl_ReturnsKeyAsIs()
    {
        const string absoluteUrl = "https://cdn.example.com/products/abc/photo.jpg";
        CdnUrlHelper.Resolve(absoluteUrl, "https://cdn.test").Should().Be(absoluteUrl);
    }

    [Fact]
    public void Resolve_EmptyCdnBaseUrl_ReturnsKeyAsIs()
    {
        const string storageKey = "products/abc/123-photo.jpg";
        CdnUrlHelper.Resolve(storageKey, null).Should().Be(storageKey);
        CdnUrlHelper.Resolve(storageKey, "").Should().Be(storageKey);
    }

    [Fact]
    public void Resolve_StorageKeyWithCdnBaseUrl_ReturnsConcatenatedUrl()
    {
        const string storageKey = "products/abc/123-photo.jpg";
        CdnUrlHelper.Resolve(storageKey, "https://cdn.test")
            .Should().Be("https://cdn.test/products/abc/123-photo.jpg");
    }

    [Fact]
    public void Resolve_CdnBaseUrlWithTrailingSlash_NormalizesSlash()
    {
        const string storageKey = "products/abc/123-photo.jpg";
        CdnUrlHelper.Resolve(storageKey, "https://cdn.test/")
            .Should().Be("https://cdn.test/products/abc/123-photo.jpg");
    }
}
