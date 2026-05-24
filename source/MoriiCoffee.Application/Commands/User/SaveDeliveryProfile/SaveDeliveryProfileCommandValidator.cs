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

        RuleFor(x => x.ProvinceName)
            .MaximumLength(200).WithMessage("Province name must not exceed 200 characters.")
            .When(x => x.ProvinceName is not null);

        RuleFor(x => x.DistrictName)
            .MaximumLength(200).WithMessage("District name must not exceed 200 characters.")
            .When(x => x.DistrictName is not null);

        RuleFor(x => x.WardCode)
            .MaximumLength(50).WithMessage("Ward code must not exceed 50 characters.")
            .When(x => x.WardCode is not null);

        RuleFor(x => x.WardName)
            .MaximumLength(200).WithMessage("Ward name must not exceed 200 characters.")
            .When(x => x.WardName is not null);
    }
}
