using FluentValidation;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileRefundPayment;

public class ReconcileRefundPaymentCommandValidator : AbstractValidator<ReconcileRefundPaymentCommand>
{
    public ReconcileRefundPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
