using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Auth.ResetPassword;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    private static ResetPasswordCommand ValidCommand() => new()
    {
        Ticket = "valid-opaque-ticket",
        NewPassword = "StrongPass1!"
    };

    [Fact]
    public void Validate_EmptyTicket_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Ticket = "";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Ticket);
    }

    [Fact]
    public void Validate_PasswordTooShort_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.NewPassword = "Ab1!";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.NewPassword = "lowercase1!";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_PasswordMissingSpecialChar_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.NewPassword = "NoSpecial1A";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OptionalEmailNull_NoError()
    {
        var cmd = ValidCommand();
        cmd.Email = null;
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
