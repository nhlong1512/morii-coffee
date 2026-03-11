using FluentValidation;

namespace MoriiCoffee.Application.Commands.User.UpdateProfile;

/// <summary>Validates UpdateProfileCommand: Bio max 1000 chars, FullName max 200 chars (when provided).</summary>
public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Bio)
            .MaximumLength(1000).WithMessage("Bio must not exceed 1000 characters.")
            .When(x => x.Bio is not null);

        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.")
            .When(x => x.FullName is not null);
    }
}
