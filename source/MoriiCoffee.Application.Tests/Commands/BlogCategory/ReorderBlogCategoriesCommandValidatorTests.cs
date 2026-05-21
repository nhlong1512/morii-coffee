using FluentAssertions;
using MoriiCoffee.Application.Commands.BlogCategory.ReorderBlogCategories;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.BlogCategory;

public class ReorderBlogCategoriesCommandValidatorTests
{
    private readonly ReorderBlogCategoriesCommandValidator _validator = new();

    [Fact]
    public void Validate_DuplicateIds_ReturnsError()
    {
        var id = Guid.NewGuid();
        var result = _validator.Validate(new ReorderBlogCategoriesCommand(new ReorderBlogCategoriesDto
        {
            Items = new List<ReorderBlogCategoriesItemDto>
            {
                new() { Id = id, DisplayOrder = 1 },
                new() { Id = id, DisplayOrder = 2 }
            }
        }));

        result.IsValid.Should().BeFalse();
    }
}
