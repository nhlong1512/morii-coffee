using FluentValidation;

namespace MoriiCoffee.Application.Commands.BlogPost.ReorderBlogPosts;

/// <summary>
/// Validates the batch blog-post reorder payload.
/// </summary>
public class ReorderBlogPostsCommandValidator : AbstractValidator<ReorderBlogPostsCommand>
{
    public ReorderBlogPostsCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one blog post reorder item is required.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(item => item.Id).Distinct().Count() == items.Count)
            .WithMessage("Blog post reorder items must not contain duplicate IDs.")
            .When(x => x.Items.Count > 0);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Blog post ID is required.");

            item.RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order must be a non-negative number.");
        });
    }
}
