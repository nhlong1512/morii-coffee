using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoriiCoffee.Application.Commands.Shipping.HandleShippingWebhookEvent;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Shipping;

public class HandleShippingWebhookEventCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IShipmentRepository> _shipments = new();
    private readonly Mock<IShipmentWebhookEventRepository> _webhookEvents = new();
    private readonly HandleShippingWebhookEventCommandHandler _handler;

    public HandleShippingWebhookEventCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.Shipments).Returns(_shipments.Object);
        _unitOfWork.Setup(x => x.ShipmentWebhookEvents).Returns(_webhookEvents.Object);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

        _handler = new HandleShippingWebhookEventCommandHandler(
            _unitOfWork.Object,
            new ShipmentStatusMapper(),
            NullLogger<HandleShippingWebhookEventCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_KnownShipment_UpdatesStatus()
    {
        var shipment = Shipment.CreatePending(Guid.NewGuid(), "MORII-1", "sandbox", 0, 200400, 53320, 2);
        shipment.MarkCreated("GHN12345", "created", 25_000m, DateTime.UtcNow.AddDays(2), null, "{}", DateTime.UtcNow);

        _webhookEvents.Setup(x => x.ExistsAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), "switch_status"))
            .ReturnsAsync(false);
        _shipments.Setup(x => x.GetByProviderOrderCodeAsync("GHN12345")).ReturnsAsync(shipment);

        var result = await _handler.Handle(new HandleShippingWebhookEventCommand
        {
            RawBody = """
                      {
                        "Type": "switch_status",
                        "OrderCode": "GHN12345",
                        "ClientOrderCode": "MORII-1",
                        "Status": "delivering",
                        "Time": "2026-05-24T12:00:00Z",
                        "Reason": ""
                      }
                      """
        }, CancellationToken.None);

        result.Result.Should().Be("processed");
        shipment.Status.Should().Be(EShipmentStatus.DELIVERING);
        _shipments.Verify(x => x.Update(shipment), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateWebhook_ReturnsDuplicate()
    {
        _webhookEvents.Setup(x => x.ExistsAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), "switch_status"))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new HandleShippingWebhookEventCommand
        {
            RawBody = """
                      {
                        "Type": "switch_status",
                        "OrderCode": "GHN12345",
                        "ClientOrderCode": "MORII-1",
                        "Status": "delivering",
                        "Time": "2026-05-24T12:00:00Z"
                      }
                      """
        }, CancellationToken.None);

        result.Result.Should().Be("duplicate");
        _shipments.Verify(x => x.Update(It.IsAny<Shipment>()), Times.Never);
    }
}
