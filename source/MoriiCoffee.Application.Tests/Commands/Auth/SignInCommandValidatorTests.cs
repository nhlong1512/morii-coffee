using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Auth.SignIn;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class SignInCommandValidatorTests
{
    private readonly SignInCommandValidator _validator = new();

    private static SignInCommand ValidCommand() => new()
    {
        Identity = "user@morii.coffee",
        Password = "AnyPass1!"
    };

    [Fact]
    public void Validate_EmptyIdentity_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Identity = "";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Identity);
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Identity = "not-an-email";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Identity);
    }

    [Fact]
    public void Validate_EmptyPassword_ReturnsError()
    {
        var cmd = ValidCommand();
        cmd.Password = "";
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }
}
