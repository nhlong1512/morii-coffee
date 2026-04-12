using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.User.UpdateProfile;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.User;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_FullNameExceeds200Chars_ReturnsError()
    {
        var cmd = new UpdateProfileCommand { FullName = new string('a', 201) };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Validate_BioExceeds1000Chars_ReturnsError()
    {
        var cmd = new UpdateProfileCommand { Bio = new string('a', 1001) };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Bio);
    }

    [Fact]
    public void Validate_NullFullName_NoError()
    {
        var cmd = new UpdateProfileCommand { FullName = null };
        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Validate_NullBio_NoError()
    {
        var cmd = new UpdateProfileCommand { Bio = null };
        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.Bio);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        var cmd = new UpdateProfileCommand
        {
            UserId = Guid.NewGuid(),
            FullName = "Nguyen Van A",
            Bio = "Coffee enthusiast"
        };
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
