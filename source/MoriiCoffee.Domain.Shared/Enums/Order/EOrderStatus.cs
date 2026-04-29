namespace MoriiCoffee.Domain.Shared.Enums.Order;

/// <summary>
/// Represents the lifecycle status of a customer order from placement to completion or cancellation.
/// </summary>
public enum EOrderStatus
{
    /// <summary>Order has been placed and is awaiting confirmation.</summary>
    PENDING = 1,

    /// <summary>Order has been confirmed by the shop.</summary>
    CONFIRMED = 2,

    /// <summary>Order is prepared and ready for customer pickup or handoff to delivery.</summary>
    READY_TO_PICKUP = 3,

    /// <summary>Order is currently being delivered to the customer.</summary>
    IN_DELIVERY = 4,

    /// <summary>Order has been successfully delivered to the customer.</summary>
    DELIVERED = 5,

    /// <summary>Customer has submitted a review for the delivered order.</summary>
    REVIEWED = 6,

    /// <summary>Order was cancelled before delivery.</summary>
    CANCELLED = 7
}
