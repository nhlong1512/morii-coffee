using FluentAssertions;
using MoriiCoffee.Application.Commands.Store.UpdateStoreStatus;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Store;

public class UpdateStoreStatusCommandValidatorTests
{
    private readonly UpdateStoreStatusCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyId_ReturnsError()
    {
        var command = new UpdateStoreStatusCommand(Guid.Empty, new UpdateStoreStatusDto { IsActive = true });

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Id");
    }
}
