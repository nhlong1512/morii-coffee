using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;

/// <summary>
/// Validates ExternalLoginCallbackCommand.
/// OAuth code/state validation is handled by authentication middleware.
/// This validator only ensures ReturnUrl is provided.
/// </summary>
public class ExternalLoginCallbackCommandValidator : AbstractValidator<ExternalLoginCallbackCommand>
{
    public ExternalLoginCallbackCommandValidator()
    {
        RuleFor(x => x.ReturnUrl)
            .NotEmpty()
            .WithMessage("'{PropertyName}' must not be empty.");
    }
}
