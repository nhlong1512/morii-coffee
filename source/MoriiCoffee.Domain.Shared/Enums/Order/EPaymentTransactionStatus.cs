namespace MoriiCoffee.Domain.Shared.Enums.Order;

/// <summary>
/// Lifecycle status for a single Payment row (one row per Stripe Checkout Session).
/// Multiple Payment rows may exist for the same Order — e.g. a failed first attempt followed
/// by a successful second attempt.
/// </summary>
public enum EPaymentTransactionStatus
{
    /// <summary>
    /// The Checkout Session has been created at Stripe but the customer has not completed it
    /// (or no webhook event has been received yet).
    /// </summary>
    Created = 1,

    /// <summary>
    /// The provider sent <c>checkout.session.completed</c> with a successful payment intent.
    /// The Order's <see cref="EPaymentStatus"/> is flipped to <see cref="EPaymentStatus.Paid"/>.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// The provider sent <c>payment_intent.payment_failed</c> after the Checkout Session had begun.
    /// The Order's <see cref="EPaymentStatus"/> is flipped to <see cref="EPaymentStatus.Failed"/>.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The provider sent <c>checkout.session.expired</c> (the customer abandoned the Stripe-hosted
    /// page or the 24-hour expiry elapsed).
    /// </summary>
    Expired = 4
}
