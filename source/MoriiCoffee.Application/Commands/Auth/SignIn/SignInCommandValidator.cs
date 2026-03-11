using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.SignIn;

/// <summary>Validates SignInCommand: ensures Identity and Password are not empty.</summary>
public class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(x => x.Identity).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
