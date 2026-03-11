using FluentValidation;

namespace MoriiCoffee.Application.Commands.Auth.RefreshToken;

/// <summary>Validates RefreshTokenCommand: ensures AccessToken and RefreshToken are not empty.</summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
