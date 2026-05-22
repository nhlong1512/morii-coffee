using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Application.SeedWork.Mappings;
using MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;
using Xunit;
using StoreEntity = MoriiCoffee.Domain.Aggregates.StoreAggregate.Store;

namespace MoriiCoffee.Application.Tests.Mappings;

public class StoreMapperTests
{
    private readonly IMapper _mapper;

    public StoreMapperTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new StoreMapper()), NullLoggerFactory.Instance);
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void StoreToDto_OrdersOpeningHoursByDayOfWeek()
    {
        var store = StoreEntity.Create(
            new MoriiCoffee.Domain.Aggregates.StoreAggregate.CreateStoreData(
                "District 1",
                null,
                "42 Nguyen Hue",
                null,
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

        store.OpeningHours =
        [
            StoreOpeningHours.Create(store.Id, 4, "07:00", "21:00", false),
            StoreOpeningHours.Create(store.Id, 0, "08:00", "21:00", false),
            StoreOpeningHours.Create(store.Id, 6, "07:00", "22:00", false)
        ];

        var dto = _mapper.Map<StoreDto>(store);

        dto.OpeningHours.Select(x => x.DayOfWeek).Should().Equal(0, 4, 6);
    }
}
