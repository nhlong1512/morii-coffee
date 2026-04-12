using FluentAssertions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using Xunit;
using MoriiCoffee.Domain.SeedWork.DomainEvent;
using MoriiCoffee.Domain.Shared.Enums.Product;
using Moq;

namespace MoriiCoffee.Domain.Tests.Aggregates;

public class ProductAggregateTests
{
    private static Product CreateProduct() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Iced Latte",
        Slug = "iced-latte",
        Description = "A classic iced latte.",
        BasePrice = 55_000m,
        IsFeatured = false,
        DisplayOrder = 1
    };

    // ── Property initialization ───────────────────────────────────────

    [Fact]
    public void NewProduct_DefaultStatus_IsActive()
    {
        var product = new Product();

        product.Status.Should().Be(EProductStatus.Active);
    }

    [Fact]
    public void NewProduct_DefaultCollections_AreEmpty()
    {
        var product = new Product();

        product.ProductCategories.Should().BeEmpty();
        product.Variants.Should().BeEmpty();
        product.Images.Should().BeEmpty();
    }

    [Fact]
    public void NewProduct_PropertiesAreSetCorrectly()
    {
        var id = Guid.NewGuid();
        var product = new Product
        {
            Id = id,
            Name = "Espresso",
            Slug = "espresso",
            BasePrice = 35_000m,
            IsFeatured = true,
            DisplayOrder = 0
        };

        product.Id.Should().Be(id);
        product.Name.Should().Be("Espresso");
        product.Slug.Should().Be("espresso");
        product.BasePrice.Should().Be(35_000m);
        product.IsFeatured.Should().BeTrue();
        product.DisplayOrder.Should().Be(0);
    }

    // ── Domain Events ─────────────────────────────────────────────────

    [Fact]
    public void RaiseDomainEvent_AddsEventToCollection()
    {
        var product = CreateProduct();
        var domainEvent = new Mock<IDomainEvent>().Object;

        product.RaiseDomainEvent(domainEvent);

        product.GetDomainEvents().Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void GetDomainEvents_InitiallyEmpty()
    {
        var product = CreateProduct();

        product.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var product = CreateProduct();
        product.RaiseDomainEvent(new Mock<IDomainEvent>().Object);
        product.RaiseDomainEvent(new Mock<IDomainEvent>().Object);

        product.ClearDomainEvents();

        product.GetDomainEvents().Should().BeEmpty();
    }
}
