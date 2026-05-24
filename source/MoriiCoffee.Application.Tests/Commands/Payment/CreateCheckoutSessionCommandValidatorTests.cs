using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Payment;

public class CreateCheckoutSessionCommandValidatorTests
{
    private readonly CreateCheckoutSessionCommandValidator _validator = new();

    private static CreateCheckoutSessionCommand ValidCommand() => new()
    {
        UserId = Guid.NewGuid(),
        FullName = "Nguyen Van A",
        PhoneNumber = "0901234567",
        Address = "123 Duong ABC, Quan 1",
        ProvinceId = 79,
        ProvinceName = "Ho Chi Minh",
        DistrictId = 760,
        DistrictName = "District 1",
        WardCode = "26734",
        WardName = "Ben Nghe",
        DeliveryMethod = EDeliveryMethod.GHN_DELIVERY,
        ShippingQuoteFingerprint = "quote-test",
        ShippingServiceId = 53320,
        ShippingServiceTypeId = 2,
        ShippingServiceLabel = "GHN Chuẩn",
        ShippingFee = 15_000m,
        ShippingQuoteExpiresAt = DateTime.UtcNow.AddMinutes(15),
        ShippingProviderEnvironment = "sandbox"
    };

    [Fact]
    public void Validate_GhnDeliveryWithoutDistrict_ReturnsError()
    {
        var command = ValidCommand();
        command.DistrictId = null;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.DistrictId);
    }

    [Fact]
    public void Validate_GhnDeliveryWithoutWardName_ReturnsError()
    {
        var command = ValidCommand();
        command.WardName = string.Empty;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.WardName);
    }

    [Fact]
    public void Validate_ValidStructuredDelivery_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }
}
