using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.ForgotPassword;

/// <summary>Validates ForgotPasswordCommand: requires a valid email address.</summary>
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}
