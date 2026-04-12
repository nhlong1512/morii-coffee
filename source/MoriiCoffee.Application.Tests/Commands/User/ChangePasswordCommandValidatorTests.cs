using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.User.ChangePassword;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.User;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyCurrentPassword_ReturnsError()
    {
        var cmd = new ChangePasswordCommand { UserId = Guid.NewGuid(), CurrentPassword = "", NewPassword = "NewPass1!" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void Validate_EmptyNewPassword_ReturnsError()
    {
        var cmd = new ChangePasswordCommand { UserId = Guid.NewGuid(), CurrentPassword = "OldPass1!", NewPassword = "" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_NewPasswordTooShort_ReturnsError()
    {
        var cmd = new ChangePasswordCommand { UserId = Guid.NewGuid(), CurrentPassword = "OldPass1!", NewPassword = "Ab1!" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var cmd = new ChangePasswordCommand
        {
            UserId = Guid.NewGuid(),
            CurrentPassword = "OldPass1!",
            NewPassword = "NewStrongPass1!"
        };
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
