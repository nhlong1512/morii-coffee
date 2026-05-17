using FluentValidation;

namespace MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;

/// <summary>Validates <see cref="CreateCheckoutSessionCommand"/> before the handler runs.</summary>
public class CreateCheckoutSessionCommandValidator : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
