using FluentValidation;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileStripePayment;

public class ReconcileStripePaymentCommandValidator : AbstractValidator<ReconcileStripePaymentCommand>
{
    public ReconcileStripePaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.SessionId)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.SessionId));
    }
}
