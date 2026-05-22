using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Store.CreateStore;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using StoreEntity = MoriiCoffee.Domain.Aggregates.StoreAggregate.Store;

namespace MoriiCoffee.Application.Tests.Commands.Store;

public class CreateStoreCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IStoresRepository> _storesRepository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateStoreCommandHandler _handler;

    public CreateStoreCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.Stores).Returns(_storesRepository.Object);
        _handler = new CreateStoreCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithDuplicateSlug_ThrowsConflict()
    {
        _storesRepository
            .Setup(x => x.SlugExistsAsync("district-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateStoreCommand(BuildDto());

        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("*slug*");
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesStoreAndCommits()
    {
        StoreEntity? createdStore = null;

        _storesRepository
            .Setup(x => x.SlugExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _storesRepository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _storesRepository
            .Setup(x => x.CreateAsync(It.IsAny<StoreEntity>()))
            .Callback<StoreEntity>(store => createdStore = store)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(x => x.Map<StoreDto>(It.IsAny<StoreEntity>()))
            .Returns((StoreEntity store) => new StoreDto
            {
                Id = store.Id,
                Name = store.Name,
                Slug = store.Slug,
                OpeningHours = store.OpeningHours.Select(hours => new StoreOpeningHoursDto
                {
                    Id = hours.Id,
                    DayOfWeek = hours.DayOfWeek,
                    OpenTime = hours.OpenTime,
                    CloseTime = hours.CloseTime,
                    IsClosed = hours.IsClosed
                }).ToList()
            });

        var result = await _handler.Handle(new CreateStoreCommand(BuildDto()), CancellationToken.None);

        result.Slug.Should().Be("district-1");
        createdStore.Should().NotBeNull();
        createdStore!.OpeningHours.Should().HaveCount(7);
        _unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
    }

    private static CreateStoreDto BuildDto() => new()
    {
        Name = "District 1",
        Address = "42 Nguyen Hue",
        City = "Ho Chi Minh City",
        Latitude = 10.77,
        Longitude = 106.70,
        Phone = "+84 28 1234 5678",
        DisplayOrder = 1,
        OpeningHours = Enumerable.Range(0, 7)
            .Select(day => new CreateStoreOpeningHoursDto
            {
                DayOfWeek = day,
                OpenTime = "07:00",
                CloseTime = "21:00",
                IsClosed = false
            })
            .ToList()
    };
}
