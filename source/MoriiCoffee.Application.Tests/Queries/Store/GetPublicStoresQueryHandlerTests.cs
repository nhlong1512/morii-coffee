using AutoMapper;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.Store.GetPublicStores;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.SeedWork;
using Xunit;
using StoreEntity = MoriiCoffee.Domain.Aggregates.StoreAggregate.Store;
using StoreOpeningHoursEntity = MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities.StoreOpeningHours;

namespace MoriiCoffee.Application.Tests.Queries.Store;

public class GetPublicStoresQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IStoresRepository> _storesRepository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetPublicStoresQueryHandler _handler;

    public GetPublicStoresQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.Stores).Returns(_storesRepository.Object);
        _handler = new GetPublicStoresQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithGeoSearchAndCaseInsensitiveFilters_ReturnsSortedMatches()
    {
        var nearStore = BuildStore("District 1", "Ho Chi Minh City", 10.7739, 106.7029, 2);
        var farStore = BuildStore("Thu Duc", "Ho Chi Minh City", 10.8560, 106.7715, 1);
        var hanoiStore = BuildStore("Hoan Kiem", "Hanoi", 21.0245, 105.8412, 3);

        _storesRepository
            .Setup(x => x.FindAll(false))
            .Returns(new List<StoreEntity> { nearStore, farStore, hanoiStore }.BuildMock());
        _mapper
            .Setup(x => x.Map<StoreDto>(It.IsAny<StoreEntity>()))
            .Returns((StoreEntity store) => new StoreDto
            {
                Id = store.Id,
                Name = store.Name,
                Address = store.Address,
                City = store.City,
                Latitude = store.Latitude,
                Longitude = store.Longitude,
                DisplayOrder = store.DisplayOrder
            });

        var result = await _handler.Handle(
            new GetPublicStoresQuery(
                new PaginationFilter { TakeAll = true },
                latitude: 10.7750,
                longitude: 106.7000,
                radius: 20,
                city: "ho chi minh",
                search: "district"),
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("District 1");
        result.Items[0].DistanceKm.Should().NotBeNull();
    }

    private static StoreEntity BuildStore(string name, string city, double latitude, double longitude, int displayOrder)
    {
        var store = StoreEntity.Create(
            new MoriiCoffee.Domain.Aggregates.StoreAggregate.CreateStoreData(
                name,
                null,
                $"{name} Address",
                null,
                city,
                null,
                latitude,
                longitude,
                "+84 28 1234 5678",
                null,
                null,
                true,
                displayOrder),
            name.ToLowerInvariant().Replace(' ', '-'));

        store.OpeningHours = Enumerable.Range(0, 7)
            .Select(day => StoreOpeningHoursEntity.Create(store.Id, day, "07:00", "21:00", false))
            .ToList();

        return store;
    }
}
