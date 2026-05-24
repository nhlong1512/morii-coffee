namespace MoriiCoffee.Domain.Shared.Enums.Shipping;

/// <summary>
/// Normalized shipment lifecycle states used by Morii.
/// </summary>
public enum EShipmentStatus
{
    NOT_REQUIRED = 1,
    QUOTE_PENDING = 2,
    QUOTED = 3,
    CREATE_PENDING = 4,
    CREATED = 5,
    READY_TO_PICK = 6,
    PICKING = 7,
    PICKED = 8,
    STORING = 9,
    TRANSPORTING = 10,
    SORTING = 11,
    DELIVERING = 12,
    DELIVERED = 13,
    CANCELLED = 14,
    DELIVERY_FAILED = 15,
    RETURNING = 16,
    RETURNED = 17,
    FAILED_TO_CREATE = 18,
    SYNC_ERROR = 19
}
