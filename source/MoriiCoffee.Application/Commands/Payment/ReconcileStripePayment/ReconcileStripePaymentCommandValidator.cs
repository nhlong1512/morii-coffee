using FluentValidation;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileStripePayment;

public class ReconcileStripePaymentCommandValidator : AbstractValidator<ReconcileStripePaymentCommand>
{
    public ReconcileStripePaymentCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.SessionId) || x.CheckoutDraftId is Guid draftId && draftId != Guid.Empty)
            .WithMessage("Either SessionId or CheckoutDraftId is required.");

        RuleFor(x => x.SessionId)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.SessionId));
    }
}
