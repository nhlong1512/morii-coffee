using FluentValidation;

namespace MoriiCoffee.Application.Commands.Payment.RefundPayment;

/// <summary>Validates <see cref="RefundPaymentCommand"/> at the request boundary.</summary>
public class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.AdminUserId)
            .NotEmpty().WithMessage("AdminUserId is required.");

        When(x => x.Amount.HasValue, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .GreaterThan(0).WithMessage("Refund amount must be greater than zero.");
        });

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
