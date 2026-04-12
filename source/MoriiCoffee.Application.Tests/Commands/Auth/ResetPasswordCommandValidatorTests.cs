using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Auth.ResetPassword;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    private static ResetPasswordCommand ValidCommand() => new()
    {
        Email = "user@morii.coffee",
        Token = "valid-reset-token",
        NewPassword = "StrongPass1!"
    };

    [Fact]
    public void Validate_EmptyEmail_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Email = "";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Email = "not-email";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmptyToken_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Token = "";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Token);
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
}
