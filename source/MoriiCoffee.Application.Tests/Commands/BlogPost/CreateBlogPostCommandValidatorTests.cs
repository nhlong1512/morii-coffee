using FluentAssertions;
using MoriiCoffee.Application.Commands.BlogPost.CreateBlogPost;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class CreateBlogPostCommandValidatorTests
{
    private readonly CreateBlogPostCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyTitle_ReturnsError()
    {
        var command = new CreateBlogPostCommand(new CreateBlogPostDto
        {
            Title = string.Empty,
            ContentHtml = "<p>x</p>"
        });

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_PublishedWithoutCategory_ReturnsError()
    {
        var command = new CreateBlogPostCommand(new CreateBlogPostDto
        {
            Title = "Post",
            ContentHtml = "<p>x</p>",
            ContentJson = "{\"type\":\"doc\"}",
            Status = EBlogPostStatus.Published
        });

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
