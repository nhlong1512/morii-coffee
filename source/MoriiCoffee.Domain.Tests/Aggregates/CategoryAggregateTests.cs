using FluentAssertions;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using Xunit;
using MoriiCoffee.Domain.SeedWork.DomainEvent;
using Moq;

namespace MoriiCoffee.Domain.Tests.Aggregates;

public class CategoryAggregateTests
{
    private static Category CreateCategory() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Espresso Drinks",
        Description = "Hot and iced espresso-based beverages.",
        DisplayOrder = 1
    };

    // ── Property initialization ───────────────────────────────────────

    [Fact]
    public void NewCategory_DefaultIsActive_IsTrue()
    {
        var category = new Category();

        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NewCategory_DefaultProductCategories_IsEmpty()
    {
        var category = new Category();

        category.ProductCategories.Should().BeEmpty();
    }

    [Fact]
    public void NewCategory_PropertiesAreSetCorrectly()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Name = "Cold Brew",
            Description = "Slow-steeped cold coffee drinks.",
            IconUrl = "https://cdn.morii.coffee/icons/cold-brew.png",
            DisplayOrder = 3
        };

        category.Id.Should().Be(id);
        category.Name.Should().Be("Cold Brew");
        category.Description.Should().Be("Slow-steeped cold coffee drinks.");
        category.IconUrl.Should().Be("https://cdn.morii.coffee/icons/cold-brew.png");
        category.DisplayOrder.Should().Be(3);
    }

    [Fact]
    public void IsActive_CanBeSetToFalse()
    {
        var category = CreateCategory();

        category.IsActive = false;

        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_CanBeToggledBackToTrue()
    {
        var category = CreateCategory();
        category.IsActive = false;

        category.IsActive = true;

        category.IsActive.Should().BeTrue();
    }

    // ── Domain Events ─────────────────────────────────────────────────

    [Fact]
    public void RaiseDomainEvent_AddsEventToCollection()
    {
        var category = CreateCategory();
        var domainEvent = new Mock<IDomainEvent>().Object;

        category.RaiseDomainEvent(domainEvent);

        category.GetDomainEvents().Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void GetDomainEvents_InitiallyEmpty()
    {
        var category = CreateCategory();

        category.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var category = CreateCategory();
        category.RaiseDomainEvent(new Mock<IDomainEvent>().Object);

        category.ClearDomainEvents();

        category.GetDomainEvents().Should().BeEmpty();
    }
}
