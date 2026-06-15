using FluentValidation;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Commands.Payment.CreateVnpayPaymentUrl;

public sealed class CreateVnpayPaymentUrlCommandValidator : AbstractValidator<CreateVnpayPaymentUrlCommand>
{
    public CreateVnpayPaymentUrlCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.IpAddress).NotEmpty().MaximumLength(45);
        When(x => x.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY, () =>
        {
            RuleFor(x => x.ShippingQuoteFingerprint).NotEmpty();
            RuleFor(x => x.ShippingFee).NotNull().GreaterThanOrEqualTo(0);
            RuleFor(x => x.ShippingQuoteExpiresAt).NotNull();
        });
    }
}
