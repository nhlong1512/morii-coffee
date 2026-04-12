using FluentAssertions;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using Xunit;
using MoriiCoffee.Domain.SeedWork.DomainEvent;
using Moq;

namespace MoriiCoffee.Domain.Tests.Aggregates;

public class BannerAggregateTests
{
    private static Banner CreateBanner() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Summer Refreshers",
        Subtitle = "Beat the heat with our iced beverages.",
        Cta = "Order Now",
        CtaLink = "/menu/cold",
        DisplayOrder = 1
    };

    // ── Property initialization ───────────────────────────────────────

    [Fact]
    public void NewBanner_DefaultIsActive_IsTrue()
    {
        var banner = new Banner();

        banner.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NewBanner_DefaultDates_AreNull()
    {
        var banner = new Banner();

        banner.StartDate.Should().BeNull();
        banner.EndDate.Should().BeNull();
    }

    [Fact]
    public void NewBanner_PropertiesAreSetCorrectly()
    {
        var id = Guid.NewGuid();
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc);

        var banner = new Banner
        {
            Id = id,
            Title = "Mùa Hè",
            Subtitle = "Khuyến mãi mùa hè",
            Cta = "Xem ngay",
            CtaLink = "/promotions/summer",
            ImageUrl = "https://cdn.morii.coffee/banners/summer.jpg",
            DisplayOrder = 0,
            StartDate = start,
            EndDate = end,
            IsActive = true
        };

        banner.Id.Should().Be(id);
        banner.Title.Should().Be("Mùa Hè");
        banner.Subtitle.Should().Be("Khuyến mãi mùa hè");
        banner.Cta.Should().Be("Xem ngay");
        banner.CtaLink.Should().Be("/promotions/summer");
        banner.ImageUrl.Should().Be("https://cdn.morii.coffee/banners/summer.jpg");
        banner.DisplayOrder.Should().Be(0);
        banner.StartDate.Should().Be(start);
        banner.EndDate.Should().Be(end);
        banner.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_CanBeSetToFalse()
    {
        var banner = CreateBanner();

        banner.IsActive = false;

        banner.IsActive.Should().BeFalse();
    }

    // ── Domain Events ─────────────────────────────────────────────────

    [Fact]
    public void RaiseDomainEvent_AddsEventToCollection()
    {
        var banner = CreateBanner();
        var domainEvent = new Mock<IDomainEvent>().Object;

        banner.RaiseDomainEvent(domainEvent);

        banner.GetDomainEvents().Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void GetDomainEvents_InitiallyEmpty()
    {
        var banner = CreateBanner();

        banner.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var banner = CreateBanner();
        banner.RaiseDomainEvent(new Mock<IDomainEvent>().Object);
        banner.RaiseDomainEvent(new Mock<IDomainEvent>().Object);

        banner.ClearDomainEvents();

        banner.GetDomainEvents().Should().BeEmpty();
    }
}
