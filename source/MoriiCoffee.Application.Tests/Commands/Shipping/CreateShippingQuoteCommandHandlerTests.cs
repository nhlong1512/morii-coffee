using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Shipping.CreateShippingQuote;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Shipping;

public class CreateShippingQuoteCommandHandlerTests
{
    private readonly Mock<ICartService> _cartService = new();
    private readonly Mock<IShippingGateway> _shippingGateway = new();
    private readonly GhnSettings _settings = new()
    {
        ShopId = 200400,
        FromDistrictId = 1461,
        FromWardCode = "21310",
        Environment = "sandbox"
    };

    private readonly CreateShippingQuoteCommandHandler _handler;

    public CreateShippingQuoteCommandHandlerTests()
    {
        _handler = new CreateShippingQuoteCommandHandler(
            _cartService.Object,
            _shippingGateway.Object,
            _settings,
            new ShippingPackageMetricsService(),
            new ShippingQuoteFingerprintService());
    }

    [Fact]
    public async Task Handle_EmptyCart_ThrowsBadRequest()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(x => x.GetCartAsync(userId)).ReturnsAsync(new CartDto());

        var act = () => _handler.Handle(new CreateShippingQuoteCommand
        {
            UserId = userId,
            DeliveryMethod = EDeliveryMethod.GHN_DELIVERY,
            PaymentMethod = EPaymentMethod.COD,
            Address = BuildAddress()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Cart is empty*");
    }

    [Fact]
    public async Task Handle_ValidRoute_ReturnsNormalizedQuote()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(x => x.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _shippingGateway.Setup(x => x.GetAvailableServicesAsync(It.IsAny<ShippingGatewayAvailableServicesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ShippingGatewayService { ServiceId = 53320, ServiceTypeId = 2, ShortName = "Chuẩn", DisplayName = "GHN Chuẩn" },
                new ShippingGatewayService { ServiceId = 53319, ServiceTypeId = 1, ShortName = "Nhanh", DisplayName = "GHN Nhanh" }
            ]);
        _shippingGateway.Setup(x => x.CalculateLeadTimeAsync(It.IsAny<ShippingGatewayLeadTimeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingGatewayLeadTimeQuote { EstimatedDeliveryAtUtc = DateTime.UtcNow.AddDays(2), RawPayload = "{}" });
        _shippingGateway.Setup(x => x.CalculateFeeAsync(It.IsAny<ShippingGatewayFeeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingGatewayFeeQuote
            {
                TotalFee = 25_000,
                ServiceFee = 20_000,
                InsuranceFee = 5_000,
                RawPayload = "{\"code\":200}"
            });

        var result = await _handler.Handle(new CreateShippingQuoteCommand
        {
            UserId = userId,
            DeliveryMethod = EDeliveryMethod.GHN_DELIVERY,
            PaymentMethod = EPaymentMethod.COD,
            Address = BuildAddress(),
            SelectedServiceId = 53320
        }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Provider.Should().Be(EShippingProvider.GHN);
        result.Environment.Should().Be("sandbox");
        result.Service.ServiceId.Should().Be(53320);
        result.FeeBreakdown.TotalFee.Should().Be(25_000);
        result.AvailableServices.Should().HaveCount(2);
        result.QuoteFingerprint.Should().NotBeNullOrWhiteSpace();
    }

    private static CartDto BuildCart() => new()
    {
        Items =
        [
            new CartItemDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "A-Mê",
                UnitPrice = 49_000m,
                Quantity = 2
            }
        ]
    };

    private static ShippingAddressDto BuildAddress() => new()
    {
        FullName = "Morii Customer",
        PhoneNumber = "0901234567",
        AddressLine = "237/65 Pham Van Chieu",
        ProvinceId = 202,
        ProvinceName = "Ho Chi Minh",
        DistrictId = 1461,
        DistrictName = "Go Vap",
        WardCode = "21310",
        WardName = "Ward 14"
    };
}
