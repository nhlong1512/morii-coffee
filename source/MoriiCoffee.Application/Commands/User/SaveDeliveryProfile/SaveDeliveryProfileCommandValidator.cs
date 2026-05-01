using FluentValidation;

namespace MoriiCoffee.Application.Commands.User.SaveDeliveryProfile;

/// <summary>Validates input for <see cref="SaveDeliveryProfileCommand"/>.</summary>
public class SaveDeliveryProfileCommandValidator : AbstractValidator<SaveDeliveryProfileCommand>
{
    public SaveDeliveryProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(15).WithMessage("Phone number must not exceed 15 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");
    }
}
