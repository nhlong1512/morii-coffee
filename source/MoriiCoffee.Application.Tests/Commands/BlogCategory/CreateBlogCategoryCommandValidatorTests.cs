using FluentAssertions;
using MoriiCoffee.Application.Commands.BlogCategory.CreateBlogCategory;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.BlogCategory;

public class CreateBlogCategoryCommandValidatorTests
{
    private readonly CreateBlogCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var result = _validator.Validate(new CreateBlogCategoryCommand(new CreateBlogCategoryDto { Name = string.Empty }));
        result.IsValid.Should().BeFalse();
    }
}
