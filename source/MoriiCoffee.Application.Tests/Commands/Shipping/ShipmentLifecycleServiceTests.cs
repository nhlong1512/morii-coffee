using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Commands.Shipping;

public class ShipmentLifecycleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IShipmentRepository> _shipments = new();
    private readonly Mock<IShippingGateway> _gateway = new();
    private readonly ShipmentLifecycleService _service;

    public ShipmentLifecycleServiceTests()
    {
        _unitOfWork.Setup(x => x.Shipments).Returns(_shipments.Object);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

        _service = new ShipmentLifecycleService(
            _unitOfWork.Object,
            _gateway.Object,
            new GhnSettings
            {
                ShopId = 200400,
                FromDistrictId = 1461,
                FromWardCode = "21310",
                Environment = "sandbox"
            },
            new ShippingPackageMetricsService(),
            new ShipmentClientOrderCodeGenerator(),
            new ShipmentStatusMapper(),
            new ShippingQuoteFingerprintService(),
            NullLogger<ShipmentLifecycleService>.Instance);
    }

    [Fact]
    public async Task TryCreateForOrderAsync_NewShipment_PersistsCreatedState()
    {
        var order = BuildGhnOrder();
        _shipments.Setup(x => x.GetByOrderIdAsync(order.Id)).ReturnsAsync((Shipment?)null);
        _gateway.Setup(x => x.CreateShipmentAsync(It.IsAny<ShippingGatewayCreateShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingGatewayCreateShipmentResult
            {
                ProviderOrderCode = "GHN12345",
                Status = "ready_to_pick",
                StatusLabel = "ready_to_pick",
                TotalFee = 25_000m,
                ExpectedDeliveryAtUtc = DateTime.UtcNow.AddDays(2),
                RawPayload = "{\"code\":200}"
            });

        var result = await _service.TryCreateForOrderAsync(order, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProviderOrderCode.Should().Be("GHN12345");
        result.Status.Should().Be(EShipmentStatus.CREATED);
        result.FeeTotal.Should().Be(25_000m);
        _shipments.Verify(x => x.CreateAsync(It.IsAny<Shipment>()), Times.Once);
        _shipments.Verify(x => x.Update(It.IsAny<Shipment>()), Times.Once);
    }

    [Fact]
    public async Task TryCreateForOrderAsync_GatewayFails_MarksShipmentFailed()
    {
        var order = BuildGhnOrder();
        _shipments.Setup(x => x.GetByOrderIdAsync(order.Id)).ReturnsAsync((Shipment?)null);
        _gateway.Setup(x => x.CreateShipmentAsync(It.IsAny<ShippingGatewayCreateShipmentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sandbox unhappy"));

        var result = await _service.TryCreateForOrderAsync(order, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be(EShipmentStatus.FAILED_TO_CREATE);
        result.FailureReason.Should().Contain("sandbox unhappy");
    }

    [Fact]
    public async Task TryCreateForOrderAsync_ExistingActiveShipment_ReturnsWithoutCreatingDuplicate()
    {
        var order = BuildGhnOrder();
        var existing = Shipment.CreatePending(order.Id, "MORII-EXISTING", "sandbox", order.Total, 200400, 53320, 2);
        existing.MarkCreated("GHN-EXISTING", "ready_to_pick", 25_000m, DateTime.UtcNow.AddDays(1), null, "{}", DateTime.UtcNow);

        _shipments.Setup(x => x.GetByOrderIdAsync(order.Id)).ReturnsAsync(existing);

        var result = await _service.TryCreateForOrderAsync(order, CancellationToken.None);

        result.Should().BeSameAs(existing);
        _gateway.Verify(x => x.CreateShipmentAsync(It.IsAny<ShippingGatewayCreateShipmentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _shipments.Verify(x => x.CreateAsync(It.IsAny<Shipment>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_UpdatesShipmentFromProviderDetail()
    {
        var order = BuildGhnOrder();
        var shipment = Shipment.CreatePending(order.Id, "MORII-SYNC", "sandbox", order.Total, 200400, 53320, 2);
        shipment.MarkCreated("GHN-SYNC", "ready_to_pick", 25_000m, DateTime.UtcNow.AddDays(1), null, "{}", DateTime.UtcNow);

        _gateway.Setup(x => x.GetShipmentDetailAsync("GHN-SYNC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingGatewayShipmentDetail
            {
                ProviderOrderCode = "GHN-SYNC",
                ClientOrderCode = "MORII-SYNC",
                Status = "delivering",
                StatusLabel = "delivering",
                TotalFee = 21_000m,
                ExpectedDeliveryAtUtc = DateTime.UtcNow.AddDays(2),
                RawPayload = "{\"status\":\"delivering\"}"
            });

        var result = await _service.SyncAsync(shipment, CancellationToken.None);

        result.Status.Should().Be(EShipmentStatus.DELIVERING);
        result.FeeTotal.Should().Be(21_000m);
        _shipments.Verify(x => x.Update(shipment), Times.Once);
    }

    [Fact]
    public async Task UpdateNoteAsync_PersistsLocalNoteAfterProviderUpdate()
    {
        var order = BuildGhnOrder();
        var shipment = Shipment.CreatePending(order.Id, "MORII-NOTE", "sandbox", order.Total, 200400, 53320, 2);
        shipment.MarkCreated("GHN-NOTE", "ready_to_pick", 25_000m, DateTime.UtcNow.AddDays(1), null, "{}", DateTime.UtcNow);

        var result = await _service.UpdateNoteAsync(shipment, "Please call first", CancellationToken.None);

        result.Note.Should().Be("Please call first");
        _gateway.Verify(x => x.UpdateShipmentNoteAsync("GHN-NOTE", "Please call first", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_MarksShipmentCancelledWhenProviderAccepts()
    {
        var order = BuildGhnOrder();
        var shipment = Shipment.CreatePending(order.Id, "MORII-CANCEL", "sandbox", order.Total, 200400, 53320, 2);
        shipment.MarkCreated("GHN-CANCEL", "ready_to_pick", 25_000m, DateTime.UtcNow.AddDays(1), null, "{}", DateTime.UtcNow);
        _gateway.Setup(x => x.CancelShipmentAsync("GHN-CANCEL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingGatewayCancelShipmentResult
            {
                ProviderOrderCode = "GHN-CANCEL",
                Success = true,
                Message = "OK",
                RawPayload = "{\"message\":\"OK\"}"
            });

        var result = await _service.CancelAsync(shipment, CancellationToken.None);

        result.Status.Should().Be(EShipmentStatus.CANCELLED);
        result.FailureReason.Should().Be("OK");
    }

    [Fact]
    public async Task RequoteAsync_ReturnsFreshQuoteFromStoredOrderSnapshot()
    {
        var order = BuildGhnOrder();
        _gateway.Setup(x => x.CalculateLeadTimeAsync(It.IsAny<ShippingGatewayLeadTimeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingGatewayLeadTimeQuote
            {
                EstimatedDeliveryAtUtc = DateTime.UtcNow.AddDays(2),
                RawPayload = "{}"
            });
        _gateway.Setup(x => x.CalculateFeeAsync(It.IsAny<ShippingGatewayFeeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShippingGatewayFeeQuote
            {
                TotalFee = 21_000m,
                ServiceFee = 21_000m,
                RawPayload = "{\"code\":200}"
            });

        var result = await _service.RequoteAsync(order, CancellationToken.None);

        result.Service.ServiceId.Should().Be(53320);
        result.FeeBreakdown.TotalFee.Should().Be(21_000m);
        result.QuoteFingerprint.Should().NotBeNullOrWhiteSpace();
    }

    private static OrderEntity BuildGhnOrder()
    {
        var order = OrderEntity.Create(
            "MORII-0001",
            Guid.NewGuid(),
            new DeliveryInfo(
                "Morii Customer",
                "0901234567",
                "237/65 Pham Van Chieu",
                202,
                "Ho Chi Minh",
                1461,
                "Go Vap",
                "21310",
                "Ward 14"),
            [
                OrderItem.Create(Guid.NewGuid(), "A-Me", 49_000m, 2, null, null)
            ],
            EPaymentMethod.COD,
            deliveryMethod: EDeliveryMethod.GHN_DELIVERY);

        order.ApplyShippingQuote(
            EShippingProvider.GHN,
            "fingerprint",
            53320,
            2,
            "GHN Chuan",
            "sandbox",
            DateTime.UtcNow.AddMinutes(15),
            25_000m);

        return order;
    }
}
