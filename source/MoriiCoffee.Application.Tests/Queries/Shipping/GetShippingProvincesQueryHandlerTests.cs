using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Shipping.GetShippingProvinces;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.Shipping;

public class GetShippingProvincesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IShippingMasterDataRepository> _repository = new();
    private readonly Mock<IShippingGateway> _shippingGateway = new();
    private readonly GetShippingProvincesQueryHandler _handler;

    public GetShippingProvincesQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.ShippingMasterData).Returns(_repository.Object);
        _handler = new GetShippingProvincesQueryHandler(_unitOfWork.Object, _shippingGateway.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappedProvinceDtos()
    {
        _repository
            .Setup(x => x.GetProvincesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                ShippingProvince.Create(79, "Ho Chi Minh", "HCM")
            ]);

        var result = await _handler.Handle(new GetShippingProvincesQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].ProvinceId.Should().Be(79);
        result[0].ProvinceName.Should().Be("Ho Chi Minh");
        result[0].Code.Should().Be("HCM");
    }

    [Fact]
    public async Task Handle_WhenCacheEmpty_LoadsFromGatewayAndPersists()
    {
        _repository
            .Setup(x => x.GetProvincesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _shippingGateway
            .Setup(x => x.GetProvincesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ShippingGatewayProvince
                {
                    ProvinceId = 79,
                    ProvinceName = "Ho Chi Minh",
                    Code = "HCM",
                    IsActive = true
                }
            ]);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new GetShippingProvincesQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].ProvinceId.Should().Be(79);
        _repository.Verify(
            x => x.UpsertProvincesAsync(It.Is<IEnumerable<ShippingProvince>>(p => p.Single().ProvinceId == 79), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
    }
}
