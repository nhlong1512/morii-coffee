using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Shipping.GetShippingDistricts;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.Shipping;

public class GetShippingDistrictsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IShippingMasterDataRepository> _repository = new();
    private readonly Mock<IShippingGateway> _shippingGateway = new();
    private readonly GetShippingDistrictsQueryHandler _handler;

    public GetShippingDistrictsQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.ShippingMasterData).Returns(_repository.Object);
        _handler = new GetShippingDistrictsQueryHandler(_unitOfWork.Object, _shippingGateway.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappedDistrictDtos()
    {
        _repository
            .Setup(x => x.GetDistrictsByProvinceIdAsync(79, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                ShippingDistrict.Create(760, 79, "District 1", 1)
            ]);

        var result = await _handler.Handle(new GetShippingDistrictsQuery(79), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].DistrictId.Should().Be(760);
        result[0].ProvinceId.Should().Be(79);
        result[0].DistrictName.Should().Be("District 1");
        result[0].SupportType.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenCacheEmpty_LoadsFromGatewayAndPersists()
    {
        _repository
            .Setup(x => x.GetDistrictsByProvinceIdAsync(79, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _shippingGateway
            .Setup(x => x.GetDistrictsAsync(79, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ShippingGatewayDistrict
                {
                    DistrictId = 760,
                    ProvinceId = 79,
                    DistrictName = "District 1",
                    SupportType = 1,
                    IsActive = true
                }
            ]);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new GetShippingDistrictsQuery(79), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].DistrictId.Should().Be(760);
        _repository.Verify(
            x => x.UpsertDistrictsAsync(It.Is<IEnumerable<ShippingDistrict>>(d => d.Single().DistrictId == 760), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
    }
}
