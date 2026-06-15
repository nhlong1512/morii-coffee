namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

public sealed class VnpayPaymentUrlResponseDto
{
    public Guid CheckoutDraftId { get; set; }
    public string TxnRef { get; set; } = null!;
    public string PaymentUrl { get; set; } = null!;
    public long Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime ExpiresAtUtc { get; set; }
}
