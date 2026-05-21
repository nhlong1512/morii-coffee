using FluentAssertions;
using MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPostStatus;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class UpdateBlogPostStatusCommandValidatorTests
{
    private readonly UpdateBlogPostStatusCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyId_ReturnsError()
    {
        var command = new UpdateBlogPostStatusCommand(Guid.Empty, new UpdateBlogPostStatusDto());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
