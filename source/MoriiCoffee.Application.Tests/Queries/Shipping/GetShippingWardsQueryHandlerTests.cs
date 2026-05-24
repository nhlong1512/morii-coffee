using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Shipping.GetShippingWards;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.Shipping;

public class GetShippingWardsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IShippingMasterDataRepository> _repository = new();
    private readonly Mock<IShippingGateway> _shippingGateway = new();
    private readonly GetShippingWardsQueryHandler _handler;

    public GetShippingWardsQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.ShippingMasterData).Returns(_repository.Object);
        _handler = new GetShippingWardsQueryHandler(_unitOfWork.Object, _shippingGateway.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappedWardDtos()
    {
        _repository
            .Setup(x => x.GetWardsByDistrictIdAsync(760, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                ShippingWard.Create("26734", 760, "Ben Nghe")
            ]);

        var result = await _handler.Handle(new GetShippingWardsQuery(760), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].WardCode.Should().Be("26734");
        result[0].DistrictId.Should().Be(760);
        result[0].WardName.Should().Be("Ben Nghe");
    }

    [Fact]
    public async Task Handle_WhenCacheEmpty_LoadsFromGatewayAndPersists()
    {
        _repository
            .Setup(x => x.GetWardsByDistrictIdAsync(760, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _shippingGateway
            .Setup(x => x.GetWardsAsync(760, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ShippingGatewayWard
                {
                    WardCode = "26734",
                    DistrictId = 760,
                    WardName = "Ben Nghe",
                    IsActive = true
                }
            ]);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new GetShippingWardsQuery(760), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].WardCode.Should().Be("26734");
        _repository.Verify(
            x => x.UpsertWardsAsync(It.Is<IEnumerable<ShippingWard>>(w => w.Single().WardCode == "26734"), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
    }
}
