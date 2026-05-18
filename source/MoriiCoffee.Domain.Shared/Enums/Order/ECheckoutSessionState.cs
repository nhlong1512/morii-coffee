namespace MoriiCoffee.Domain.Shared.Enums.Order;

/// <summary>
/// Provider-side lifecycle for a hosted checkout session.
/// This is distinct from <see cref="EPaymentStatus"/> because a checkout session may exist
/// before an order is created in payment-first flows.
/// </summary>
public enum ECheckoutSessionState
{
    Pending = 1,
    Paid = 2,
    Expired = 3
}
