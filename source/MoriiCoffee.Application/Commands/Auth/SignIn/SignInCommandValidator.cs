using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.SignIn;

/// <summary>Validates SignInCommand: ensures Identity is a valid email address and Password is not empty.</summary>
public class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(x => x.Identity)
            .NotEmpty()
            .EmailAddress().WithMessage("'{PropertyName}' is not a valid email address.");

        RuleFor(x => x.Password).NotEmpty();
    }
}
