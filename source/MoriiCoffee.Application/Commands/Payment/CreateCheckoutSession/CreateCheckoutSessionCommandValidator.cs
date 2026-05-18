using FluentValidation;

namespace MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;

/// <summary>Validates <see cref="CreateCheckoutSessionCommand"/> before the handler runs.</summary>
public class CreateCheckoutSessionCommandValidator : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().MaximumLength(20);

        RuleFor(x => x.Address)
            .NotEmpty().MaximumLength(300);

        RuleFor(x => x.Notes)
            .MaximumLength(500);
    }
}
