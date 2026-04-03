using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLogin;

/// <summary>
/// Validates ExternalLoginCommand to ensure provider is supported.
/// Currently only "Google" provider is allowed. Case-insensitive validation.
/// </summary>
public class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("'{PropertyName}' must not be empty.")
            .Must(provider => provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Unsupported provider '{PropertyValue}'. Only 'Google' is supported.");

        RuleFor(x => x.ReturnUrl)
            .NotEmpty().WithMessage("'{PropertyName}' must not be empty.");
    }
}
