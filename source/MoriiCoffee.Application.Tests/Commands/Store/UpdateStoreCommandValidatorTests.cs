using FluentAssertions;
using MoriiCoffee.Application.Commands.Store.UpdateStore;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Store;

public class UpdateStoreCommandValidatorTests
{
    private readonly UpdateStoreCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyId_ReturnsError()
    {
        var command = new UpdateStoreCommand(Guid.Empty, BuildDto());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Id");
    }

    [Fact]
    public void Validate_WithInvalidTimeFormat_ReturnsError()
    {
        var dto = BuildDto();
        dto.OpeningHours[0].OpenTime = "24:00";

        var result = _validator.Validate(new UpdateStoreCommand(Guid.NewGuid(), dto));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("HH:mm"));
    }

    private static CreateStoreDto BuildDto() => new()
    {
        Name = "Morii Coffee - District 1",
        Address = "42 Nguyen Hue",
        City = "Ho Chi Minh City",
        Latitude = 10.77,
        Longitude = 106.70,
        Phone = "+84 28 1234 5678",
        DisplayOrder = 1,
        OpeningHours = Enumerable.Range(0, 7)
            .Select(day => new CreateStoreOpeningHoursDto
            {
                DayOfWeek = day,
                OpenTime = "07:00",
                CloseTime = "21:00",
                IsClosed = false
            })
            .ToList()
    };
}
