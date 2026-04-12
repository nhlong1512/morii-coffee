using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Auth.ForgotPassword;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Auth;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyEmail_ReturnsError()
    {
        var cmd = new ForgotPasswordCommand { Email = "" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ReturnsError()
    {
        var cmd = new ForgotPasswordCommand { Email = "not-email" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ValidEmail_NoErrors()
    {
        var cmd = new ForgotPasswordCommand { Email = "user@morii.coffee" };
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
