using FluentAssertions;
using MoriiCoffee.Domain.Aggregates.StoreAggregate;
using MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;
using Xunit;
using StoreEntity = MoriiCoffee.Domain.Aggregates.StoreAggregate.Store;

namespace MoriiCoffee.Domain.Tests.Aggregates;

public class StoreAggregateTests
{
    [Fact]
    public void ReplaceOpeningHours_WithExistingSevenDays_UpdatesRowsInPlace()
    {
        var store = CreateStore();
        store.OpeningHours = Enumerable.Range(0, 7)
            .Select(day => StoreOpeningHours.Create(store.Id, day, "07:00", "21:00", false))
            .ToList();
        var originalIdsByDay = store.OpeningHours.ToDictionary(hours => hours.DayOfWeek, hours => hours.Id);

        store.ReplaceOpeningHours(Enumerable.Range(0, 7)
            .Select(day => new StoreOpeningHoursData(
                day,
                day == 0 ? "08:00" : "07:30",
                day == 6 ? "22:00" : "20:30",
                day == 2)));

        store.OpeningHours.Should().HaveCount(7);
        store.OpeningHours.Select(hours => hours.DayOfWeek).Should().BeEquivalentTo(Enumerable.Range(0, 7));
        store.OpeningHours.Should().OnlyContain(hours => hours.Id == originalIdsByDay[hours.DayOfWeek]);
        store.OpeningHours.Single(hours => hours.DayOfWeek == 0).OpenTime.Should().Be("08:00");
        store.OpeningHours.Single(hours => hours.DayOfWeek == 2).IsClosed.Should().BeTrue();
        store.OpeningHours.Single(hours => hours.DayOfWeek == 6).CloseTime.Should().Be("22:00");
    }

    private static StoreEntity CreateStore() =>
        StoreEntity.Create(
            new CreateStoreData(
                "Morii Coffee - District 1",
                null,
                "42 Nguyen Hue",
                "District 1",
                "Ho Chi Minh City",
                null,
                10.77,
                106.70,
                "+84 28 1234 5678",
                null,
                null,
                true,
                1),
            "district-1");
}
