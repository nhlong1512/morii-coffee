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
    PAYPAL = 3,

    /// <summary>
    /// Stripe online card payment. After the order is placed the customer is redirected to
    /// the Stripe-hosted Checkout page; the order is confirmed as paid only when Stripe
    /// emits a <c>checkout.session.completed</c> webhook event (see feature 011-stripe-payment).
    /// </summary>
    STRIPE = 4
}
