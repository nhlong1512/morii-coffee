using FluentValidation;

namespace MoriiCoffee.Application.Commands.User.ChangePassword;

/// <summary>Validates ChangePasswordCommand: CurrentPassword non-empty, NewPassword minimum 8 characters.</summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
