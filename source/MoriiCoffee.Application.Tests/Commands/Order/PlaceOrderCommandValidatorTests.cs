using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Order.PlaceOrder;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Order;

public class PlaceOrderCommandValidatorTests
{
    private readonly PlaceOrderCommandValidator _validator = new();

    private static PlaceOrderCommand ValidCommand() => new()
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
        PaymentMethod = EPaymentMethod.COD,
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
    public void Validate_GhnDeliveryWithoutProvince_ReturnsError()
    {
        var command = ValidCommand();
        command.ProvinceId = null;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ProvinceId);
    }

    [Fact]
    public void Validate_GhnDeliveryWithoutWardCode_ReturnsError()
    {
        var command = ValidCommand();
        command.WardCode = string.Empty;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.WardCode);
    }

    [Fact]
    public void Validate_ValidStructuredDelivery_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }
}
