namespace MoriiCoffee.Domain.Shared.Enums.Order;

/// <summary>
/// Represents the payment method selected by the customer at checkout.
/// </summary>
public enum EPaymentMethod
{
    /// <summary>Cash on delivery — payment collected when the order arrives.</summary>
    COD = 1,

    /// <summary>MoMo e-wallet payment (integration planned for a future milestone).</summary>
    MOMO = 2,

    /// <summary>PayPal payment (integration planned for a future milestone).</summary>
    PAYPAL = 3
}
