using FluentValidation;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileVnpayPayment;

public sealed class ReconcileVnpayPaymentCommandValidator : AbstractValidator<ReconcileVnpayPaymentCommand>
{
    public ReconcileVnpayPaymentCommandValidator()
    {
        RuleFor(x => x).Must(x => x.CheckoutDraftId != Guid.Empty || !string.IsNullOrWhiteSpace(x.TxnRef))
            .WithMessage("checkoutDraftId or txnRef is required.");
    }
}
