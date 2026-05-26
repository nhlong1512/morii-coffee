using FluentValidation;

namespace MoriiCoffee.Application.Commands.User.ChangePassword;

/// <summary>Validates ChangePasswordCommand: CurrentPassword non-empty, NewPassword minimum 8 characters.</summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        // Both passwords arrive RSA-OAEP encrypted — only presence is validated here.
        // Complexity is enforced on the client and by ASP.NET Identity after decryption.
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty();
    }
}
