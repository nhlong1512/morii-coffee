using FluentAssertions;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using Xunit;
using MoriiCoffee.Domain.SeedWork.DomainEvent;
using MoriiCoffee.Domain.Shared.Enums.User;
using Moq;

namespace MoriiCoffee.Domain.Tests.Aggregates;

public class UserAggregateTests
{
    private static User CreateUser() => new()
    {
        Email = "test@morii.coffee",
        UserName = "testuser",
        PhoneNumber = "+84901234567"
    };

    // ── UpdateProfile ────────────────────────────────────────────────

    [Fact]
    public void UpdateProfile_SetsAllFields()
    {
        var user = CreateUser();
        var dob = new DateTime(1995, 1, 1);

        user.UpdateProfile("Nguyen Van A", dob, EGender.Male, "Coffee lover");

        user.FullName.Should().Be("Nguyen Van A");
        user.Dob.Should().Be(dob);
        user.Gender.Should().Be(EGender.Male);
        user.Bio.Should().Be("Coffee lover");
    }

    [Fact]
    public void UpdateProfile_WithNullValues_ClearsFields()
    {
        var user = CreateUser();
        user.UpdateProfile("Initial Name", DateTime.Now, EGender.Female, "Some bio");

        user.UpdateProfile(null, null, null, null);

        user.FullName.Should().BeNull();
        user.Dob.Should().BeNull();
        user.Gender.Should().BeNull();
        user.Bio.Should().BeNull();
    }

    // ── SetAvatar ────────────────────────────────────────────────────

    [Fact]
    public void SetAvatar_SetsUrlAndFileName()
    {
        var user = CreateUser();

        user.SetAvatar("https://cdn.morii.coffee/avatar.jpg", "avatars/avatar.jpg");

        user.AvatarUrl.Should().Be("https://cdn.morii.coffee/avatar.jpg");
        user.AvatarFileName.Should().Be("avatars/avatar.jpg");
    }

    [Fact]
    public void SetAvatar_WithNullValues_ClearsAvatar()
    {
        var user = CreateUser();
        user.SetAvatar("https://old-url.com/img.jpg", "old/key.jpg");

        user.SetAvatar(null, null);

        user.AvatarUrl.Should().BeNull();
        user.AvatarFileName.Should().BeNull();
    }

    // ── Activate / Deactivate ─────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsStatusToInactive()
    {
        var user = CreateUser();
        user.Status.Should().Be(EUserStatus.Active);

        user.Deactivate();

        user.Status.Should().Be(EUserStatus.Inactive);
    }

    [Fact]
    public void Activate_SetsStatusToActive()
    {
        var user = CreateUser();
        user.Deactivate();

        user.Activate();

        user.Status.Should().Be(EUserStatus.Active);
    }

    [Fact]
    public void DefaultStatus_IsActive()
    {
        var user = new User();

        user.Status.Should().Be(EUserStatus.Active);
    }

    // ── Domain Events ─────────────────────────────────────────────────

    [Fact]
    public void RaiseDomainEvent_AddsEventToCollection()
    {
        var user = CreateUser();
        var domainEvent = new Mock<IDomainEvent>().Object;

        user.RaiseDomainEvent(domainEvent);

        user.GetDomainEvents().Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public void RaiseDomainEvent_MultipleEvents_AllAreStored()
    {
        var user = CreateUser();
        var event1 = new Mock<IDomainEvent>().Object;
        var event2 = new Mock<IDomainEvent>().Object;

        user.RaiseDomainEvent(event1);
        user.RaiseDomainEvent(event2);

        user.GetDomainEvents().Should().HaveCount(2);
    }

    [Fact]
    public void GetDomainEvents_InitiallyEmpty()
    {
        var user = CreateUser();

        user.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var user = CreateUser();
        user.RaiseDomainEvent(new Mock<IDomainEvent>().Object);
        user.RaiseDomainEvent(new Mock<IDomainEvent>().Object);

        user.ClearDomainEvents();

        user.GetDomainEvents().Should().BeEmpty();
    }
}
