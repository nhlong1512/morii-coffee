using FluentValidation;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Commands.Shipping.CreateShippingQuote;

public class CreateShippingQuoteCommandValidator : AbstractValidator<CreateShippingQuoteCommand>
{
    public CreateShippingQuoteCommandValidator()
    {
        RuleFor(x => x.DeliveryMethod)
            .IsInEnum();

        RuleFor(x => x.PaymentMethod)
            .IsInEnum();

        RuleFor(x => x.Address.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Address.PhoneNumber)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.Address.AddressLine)
            .NotEmpty()
            .MaximumLength(500);

        When(x => x.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY, () =>
        {
            RuleFor(x => x.Address.ProvinceId)
                .NotNull().WithMessage("Province is required for GHN delivery.");

            RuleFor(x => x.Address.ProvinceName)
                .NotEmpty().WithMessage("Province name is required for GHN delivery.");

            RuleFor(x => x.Address.DistrictId)
                .NotNull().WithMessage("District is required for GHN delivery.");

            RuleFor(x => x.Address.DistrictName)
                .NotEmpty().WithMessage("District name is required for GHN delivery.");

            RuleFor(x => x.Address.WardCode)
                .NotEmpty().WithMessage("Ward code is required for GHN delivery.");

            RuleFor(x => x.Address.WardName)
                .NotEmpty().WithMessage("Ward name is required for GHN delivery.");
        });
    }
}
