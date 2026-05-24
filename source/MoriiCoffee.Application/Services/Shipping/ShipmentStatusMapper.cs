using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Services.Shipping;

public class ShipmentStatusMapper
{
    public EShipmentStatus Map(string? providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus))
            return EShipmentStatus.SYNC_ERROR;

        return providerStatus.Trim().ToLowerInvariant() switch
        {
            "create_pending" => EShipmentStatus.CREATE_PENDING,
            "ready_to_pick" => EShipmentStatus.READY_TO_PICK,
            "picking" => EShipmentStatus.PICKING,
            "money_collect_picking" => EShipmentStatus.PICKING,
            "picked" => EShipmentStatus.PICKED,
            "storing" => EShipmentStatus.STORING,
            "transporting" => EShipmentStatus.TRANSPORTING,
            "sorting" => EShipmentStatus.SORTING,
            "delivering" => EShipmentStatus.DELIVERING,
            "delivered" => EShipmentStatus.DELIVERED,
            "delivery_fail" => EShipmentStatus.DELIVERY_FAILED,
            "waiting_to_return" => EShipmentStatus.RETURNING,
            "return" => EShipmentStatus.RETURNING,
            "return_transporting" => EShipmentStatus.RETURNING,
            "returned" => EShipmentStatus.RETURNED,
            "cancel" => EShipmentStatus.CANCELLED,
            "exception" => EShipmentStatus.SYNC_ERROR,
            _ => EShipmentStatus.SYNC_ERROR
        };
    }
}
