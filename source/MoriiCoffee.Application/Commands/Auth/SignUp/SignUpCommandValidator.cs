using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.SignUp;

/// <summary>Validates SignUpCommand: email format, phone number, password complexity, and optional username constraints.</summary>
public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[0-9]{7,15}$").WithMessage("A valid phone number is required.");

        // Password arrives RSA-OAEP encrypted from the frontend — only presence is validated here.
        // Complexity is enforced on the client and by ASP.NET Identity after decryption.
        RuleFor(x => x.Password).NotEmpty();

        RuleFor(x => x.UserName)
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username may only contain letters, digits, and underscores.")
            .When(x => !string.IsNullOrEmpty(x.UserName));
    }
}
