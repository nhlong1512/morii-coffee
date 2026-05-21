using FluentAssertions;
using MoriiCoffee.Application.Commands.BlogPost.ReorderBlogPosts;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class ReorderBlogPostsCommandValidatorTests
{
    private readonly ReorderBlogPostsCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyItems_ReturnsError()
    {
        var command = new ReorderBlogPostsCommand(new ReorderBlogPostsDto());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DuplicateIds_ReturnsError()
    {
        var id = Guid.NewGuid();
        var command = new ReorderBlogPostsCommand(new ReorderBlogPostsDto
        {
            Items = new List<ReorderBlogPostsItemDto>
            {
                new() { Id = id, DisplayOrder = 1 },
                new() { Id = id, DisplayOrder = 2 }
            }
        });

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
