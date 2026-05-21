using FluentAssertions;
using MoriiCoffee.Application.Commands.BlogCategory.UpdateBlogCategory;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.BlogCategory;

public class UpdateBlogCategoryCommandValidatorTests
{
    private readonly UpdateBlogCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyId_ReturnsError()
    {
        var result = _validator.Validate(new UpdateBlogCategoryCommand(Guid.Empty, new UpdateBlogCategoryDto { Name = "Guides" }));
        result.IsValid.Should().BeFalse();
    }
}
