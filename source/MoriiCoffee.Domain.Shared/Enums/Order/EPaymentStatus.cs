namespace MoriiCoffee.Domain.Shared.Enums.Order;

/// <summary>
/// Represents the payment lifecycle of an order, kept distinct from <see cref="EOrderStatus"/>
/// (which tracks fulfilment). Both states evolve independently.
/// </summary>
public enum EPaymentStatus
{
    /// <summary>
    /// The order does not require online payment because it will be paid on delivery (COD).
    /// Terminal state for the entire order lifetime.
    /// </summary>
    NotRequired = 1,

    /// <summary>
    /// An online-payment session has been created at the provider but no terminal event
    /// (success, failure, expiry) has been received yet.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// The provider has confirmed that the charge succeeded; the order is paid and can advance
    /// in its fulfilment lifecycle.
    /// </summary>
    Paid = 3,

    /// <summary>
    /// The payment attempt did not succeed (card declined, session expired, asynchronous failure
    /// after acceptance, etc.). The customer or admin can cancel the order or trigger a retry.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The full amount of the order has been refunded. Terminal for an online order.
    /// </summary>
    Refunded = 5,

    /// <summary>
    /// One or more refunds have been issued but the cumulative refund is less than the total.
    /// Further refunds (up to the remaining balance) are still possible.
    /// </summary>
    PartiallyRefunded = 6
}
