using FluentValidation;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Commands.Order.PlaceOrder;

/// <summary>Validates <see cref="PlaceOrderCommand"/> before the handler executes.</summary>
public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    /// <summary>Configures validation rules for placing an order.</summary>
    public PlaceOrderCommandValidator()
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

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("A valid payment method is required.");

        When(
            x => x.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY,
            () =>
            {
                RuleFor(x => x.ProvinceId)
                    .NotNull().WithMessage("Province is required for GHN delivery.");

                RuleFor(x => x.ProvinceName)
                    .NotEmpty().WithMessage("Province name is required for GHN delivery.");

                RuleFor(x => x.DistrictId)
                    .NotNull().WithMessage("District is required for GHN delivery.");

                RuleFor(x => x.DistrictName)
                    .NotEmpty().WithMessage("District name is required for GHN delivery.");

                RuleFor(x => x.WardCode)
                    .NotEmpty().WithMessage("Ward code is required for GHN delivery.");

                RuleFor(x => x.WardName)
                    .NotEmpty().WithMessage("Ward name is required for GHN delivery.");

                RuleFor(x => x.ShippingQuoteFingerprint)
                    .NotEmpty().WithMessage("A valid shipping quote is required for GHN delivery.");

                RuleFor(x => x.ShippingServiceId)
                    .NotNull().WithMessage("Shipping service is required for GHN delivery.");

                RuleFor(x => x.ShippingFee)
                    .NotNull().WithMessage("Shipping fee is required for GHN delivery.")
                    .GreaterThanOrEqualTo(0).WithMessage("Shipping fee must not be negative.");

                RuleFor(x => x.ShippingQuoteExpiresAt)
                    .NotNull().WithMessage("Shipping quote expiry is required for GHN delivery.");

                RuleFor(x => x.ShippingProviderEnvironment)
                    .NotEmpty().WithMessage("Shipping provider environment is required for GHN delivery.");
            });
    }
}
