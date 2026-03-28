using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;

/// <summary>
/// Validates ExternalLoginCallbackCommand to ensure required OAuth parameters are present.
/// Code and State parameters are required for successful OAuth callback processing.
/// Error and ErrorDescription are optional (only present when authentication fails).
/// </summary>
public class ExternalLoginCallbackCommandValidator : AbstractValidator<ExternalLoginCallbackCommand>
{
    public ExternalLoginCallbackCommandValidator()
    {
        // Code is required unless there's an error
        RuleFor(x => x.Code)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.Error))
            .WithMessage("'{PropertyName}' must not be empty when authentication succeeds.");

        // State is required for CSRF protection
        RuleFor(x => x.State)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.Error))
            .WithMessage("'{PropertyName}' must not be empty for CSRF validation.");

        RuleFor(x => x.ReturnUrl)
            .NotEmpty().WithMessage("'{PropertyName}' must not be empty.");
    }
}
