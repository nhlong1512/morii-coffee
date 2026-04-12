using FluentAssertions;
using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Auth.SignUp;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class SignUpCommandValidatorTests
{
    private readonly SignUpCommandValidator _validator = new();

    private static SignUpCommand ValidCommand() => new(new SignUpDto
    {
        Email = "user@morii.coffee",
        PhoneNumber = "+84901234567",
        Password = "StrongPass1!",
        UserName = null
    });

    // ── Email ──────────────────────────────────────────────────────────

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
        cmd.Email = "not-an-email";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    // ── PhoneNumber ────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyPhoneNumber_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.PhoneNumber = "";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_InvalidPhoneFormat_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.PhoneNumber = "abc";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    // ── Password ───────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyPassword_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Password = "";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordTooShort_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Password = "Ab1!";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Password = "lowercase1!";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordMissingLowercase_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Password = "UPPERCASE1!";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordMissingDigit_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Password = "NoDigits!!A";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordMissingSpecialChar_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Password = "NoSpecial1A";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    // ── UserName (optional) ────────────────────────────────────────────

    [Fact]
    public void Validate_UserNameTooShort_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.UserName = "ab";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void Validate_UserNameTooLong_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.UserName = new string('a', 51);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void Validate_UserNameWithInvalidChars_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.UserName = "user name!";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Fact]
    public void Validate_NullUserName_NoError()
    {
        var cmd = ValidCommand();
        cmd.UserName = null;
        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.UserName);
    }

    // ── Valid ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var cmd = ValidCommand();
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
