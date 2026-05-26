using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.ResetPassword;

/// <summary>Validates ResetPasswordCommand: email format, token presence, and password complexity.</summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Token)
            .NotEmpty();

        // NewPassword arrives RSA-OAEP encrypted from the frontend — only presence is validated here.
        // Complexity is enforced on the client and by ASP.NET Identity after decryption.
        RuleFor(x => x.NewPassword).NotEmpty();
    }
}
