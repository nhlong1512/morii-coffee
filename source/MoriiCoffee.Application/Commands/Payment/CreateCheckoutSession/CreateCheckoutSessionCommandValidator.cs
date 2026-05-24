using FluentValidation;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

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

        RuleFor(x => x.ProvinceName)
            .MaximumLength(200)
            .When(x => x.ProvinceName is not null);

        RuleFor(x => x.DistrictName)
            .MaximumLength(200)
            .When(x => x.DistrictName is not null);

        RuleFor(x => x.WardCode)
            .MaximumLength(50)
            .When(x => x.WardCode is not null);

        RuleFor(x => x.WardName)
            .MaximumLength(200)
            .When(x => x.WardName is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(500);

        When(
            x => x.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY,
            () =>
            {
                RuleFor(x => x.ProvinceId).NotNull();
                RuleFor(x => x.ProvinceName).NotEmpty();
                RuleFor(x => x.DistrictId).NotNull();
                RuleFor(x => x.DistrictName).NotEmpty();
                RuleFor(x => x.WardCode).NotEmpty();
                RuleFor(x => x.WardName).NotEmpty();
                RuleFor(x => x.ShippingQuoteFingerprint).NotEmpty();
                RuleFor(x => x.ShippingServiceId).NotNull();
                RuleFor(x => x.ShippingFee).NotNull().GreaterThanOrEqualTo(0);
                RuleFor(x => x.ShippingQuoteExpiresAt).NotNull();
                RuleFor(x => x.ShippingProviderEnvironment).NotEmpty();
            });
    }
}
