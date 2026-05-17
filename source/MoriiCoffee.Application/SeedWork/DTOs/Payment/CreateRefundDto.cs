namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Request body for <c>POST /api/v1/payments/{orderId}/refund</c>.</summary>
public class CreateRefundDto
{
    /// <summary>
    /// Refund amount in VND. Null or zero means "full refund of remaining unrefunded balance".
    /// Cannot exceed the unrefunded balance on the order's successful payment.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>Optional admin-supplied reason recorded against the refund (max 500 chars).</summary>
    public string? Reason { get; set; }
}
