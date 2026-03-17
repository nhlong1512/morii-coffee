namespace MoriiCoffee.Domain.Shared.Enums.Payment;

/// <summary>Lifecycle status of a Stripe payment intent stored in the database.</summary>
public enum EPaymentStatus
{
    /// <summary>Payment intent created but not yet confirmed by the customer.</summary>
    Pending = 0,

    /// <summary>Payment was successfully charged.</summary>
    Succeeded = 1,

    /// <summary>Payment attempt failed (e.g., card declined).</summary>
    Failed = 2,

    /// <summary>Payment intent was canceled before completion.</summary>
    Canceled = 3,

    /// <summary>Payment was successfully refunded.</summary>
    Refunded = 4
}
