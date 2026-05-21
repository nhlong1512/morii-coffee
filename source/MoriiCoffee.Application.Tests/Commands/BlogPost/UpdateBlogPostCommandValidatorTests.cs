using FluentAssertions;
using MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPost;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class UpdateBlogPostCommandValidatorTests
{
    private readonly UpdateBlogPostCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyId_ReturnsError()
    {
        var command = new UpdateBlogPostCommand(Guid.Empty, new UpdateBlogPostDto
        {
            Title = "Post",
            ContentHtml = "<p>x</p>"
        });

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DuplicateCategoryIds_ReturnsError()
    {
        var categoryId = Guid.NewGuid();
        var command = new UpdateBlogPostCommand(Guid.NewGuid(), new UpdateBlogPostDto
        {
            Title = "Post",
            ContentHtml = "<p>x</p>",
            CategoryIds = new List<Guid> { categoryId, categoryId }
        });

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_PublishedWithoutContentJson_ReturnsError()
    {
        var command = new UpdateBlogPostCommand(Guid.NewGuid(), new UpdateBlogPostDto
        {
            Title = "Post",
            ContentHtml = "<p>x</p>",
            Status = EBlogPostStatus.Published
        });

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
