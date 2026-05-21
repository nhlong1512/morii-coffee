using FluentValidation;

namespace MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPostStatus;

/// <summary>
/// Validates the blog post status update request.
/// </summary>
public class UpdateBlogPostStatusCommandValidator : AbstractValidator<UpdateBlogPostStatusCommand>
{
    public UpdateBlogPostStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Blog post ID is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid blog post status.");
    }
}
