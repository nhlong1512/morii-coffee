using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileVnpayPayment;

public sealed class ReconcileVnpayPaymentDto
{
    public Guid CheckoutDraftId { get; set; }
    public string? TxnRef { get; set; }
}

public sealed class ReconcileVnpayPaymentCommand : ICommand<ReconcileVnpayPaymentResponseDto>
{
    public Guid CheckoutDraftId { get; set; }
    public string? TxnRef { get; set; }
    public Guid RequestingUserId { get; set; }
    public bool IsAdmin { get; set; }
}

public sealed class ReconcileVnpayPaymentResponseDto
{
    public Guid CheckoutDraftId { get; set; }
    public string TxnRef { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public EPaymentStatus PaymentStatus { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
