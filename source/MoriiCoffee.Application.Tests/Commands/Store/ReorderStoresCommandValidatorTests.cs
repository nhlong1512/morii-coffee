using FluentAssertions;
using MoriiCoffee.Application.Commands.Store.ReorderStores;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Store;

public class ReorderStoresCommandValidatorTests
{
    private readonly ReorderStoresCommandValidator _validator = new();

    [Fact]
    public void Validate_WithDuplicateIds_ReturnsError()
    {
        var id = Guid.NewGuid();
        var command = new ReorderStoresCommand(new ReorderStoresDto
        {
            Items =
            [
                new ReorderStoreItem { Id = id, DisplayOrder = 1 },
                new ReorderStoreItem { Id = id, DisplayOrder = 2 }
            ]
        });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("duplicate IDs"));
    }

    [Fact]
    public void Validate_WithNegativeDisplayOrder_ReturnsError()
    {
        var command = new ReorderStoresCommand(new ReorderStoresDto
        {
            Items = [new ReorderStoreItem { Id = Guid.NewGuid(), DisplayOrder = -1 }]
        });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("non-negative"));
    }
}
