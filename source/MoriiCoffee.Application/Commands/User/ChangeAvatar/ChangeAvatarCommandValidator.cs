using FluentValidation;

namespace MoriiCoffee.Application.Commands.User.ChangeAvatar;

/// <summary>Validates ChangeAvatarCommand: Avatar file must be present and have an image/* content type.</summary>
public class ChangeAvatarCommandValidator : AbstractValidator<ChangeAvatarCommand>
{
    public ChangeAvatarCommandValidator()
    {
        RuleFor(x => x.Avatar)
            .NotNull().WithMessage("Avatar file is required.")
            .Must(f => f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                .WithMessage("File must be an image (jpeg, png, webp, etc.).");
    }
}
