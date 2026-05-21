using FluentValidation;
using MoriiCoffee.Domain.Shared.Enums.Blog;

namespace MoriiCoffee.Application.Commands.BlogPost.CreateBlogPost;

/// <summary>
/// Validates scalar fields for creating a blog post.
/// Business-rule checks involving repository lookups are handled in the command handler.
/// </summary>
public class CreateBlogPostCommandValidator : AbstractValidator<CreateBlogPostCommand>
{
    public CreateBlogPostCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Blog post title is required.")
            .MaximumLength(200).WithMessage("Blog post title must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Excerpt)
            .MaximumLength(1000).WithMessage("Excerpt must not exceed 1000 characters.")
            .When(x => x.Excerpt != null);

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(500).WithMessage("Cover image URL must not exceed 500 characters.")
            .When(x => x.CoverImageUrl != null);

        RuleFor(x => x.CoverImageFileName)
            .MaximumLength(500).WithMessage("Cover image file name must not exceed 500 characters.")
            .When(x => x.CoverImageFileName != null);

        RuleFor(x => x.SeoTitle)
            .MaximumLength(200).WithMessage("SEO title must not exceed 200 characters.")
            .When(x => x.SeoTitle != null);

        RuleFor(x => x.SeoDescription)
            .MaximumLength(500).WithMessage("SEO description must not exceed 500 characters.")
            .When(x => x.SeoDescription != null);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be a non-negative number.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid blog post status.");

        RuleFor(x => x.ContentHtml)
            .NotNull().WithMessage("ContentHtml is required.");

        RuleFor(x => x.CategoryIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Category IDs must not contain duplicates.")
            .When(x => x.CategoryIds.Count > 0);

        When(x => x.Status == EBlogPostStatus.Published, () =>
        {
            RuleFor(x => x.ContentHtml)
                .Must(content => !string.IsNullOrWhiteSpace(content))
                .WithMessage("Published blog posts must have HTML content.");

            RuleFor(x => x.ContentJson)
                .Must(content => !string.IsNullOrWhiteSpace(content))
                .WithMessage("Published blog posts must have editor JSON content.");

            RuleFor(x => x.CategoryIds)
                .NotEmpty().WithMessage("Published blog posts must have at least one category.");
        });
    }
}
