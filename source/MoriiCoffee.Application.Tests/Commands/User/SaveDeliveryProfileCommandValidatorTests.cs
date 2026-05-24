using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.User.SaveDeliveryProfile;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.User;

public class SaveDeliveryProfileCommandValidatorTests
{
    private readonly SaveDeliveryProfileCommandValidator _validator = new();

    private static SaveDeliveryProfileCommand ValidCommand() => new()
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
        WardName = "Ben Nghe"
    };

    [Fact]
    public void Validate_EmptyFullName_ReturnsError()
    {
        var command = ValidCommand();
        command.FullName = string.Empty;

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Validate_WardCodeTooLong_ReturnsError()
    {
        var command = ValidCommand();
        command.WardCode = new string('A', 51);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.WardCode);
    }

    [Fact]
    public void Validate_ValidStructuredAddress_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }
}
