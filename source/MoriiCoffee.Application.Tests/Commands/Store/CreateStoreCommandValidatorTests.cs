using FluentAssertions;
using MoriiCoffee.Application.Commands.Store.CreateStore;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Store;

public class CreateStoreCommandValidatorTests
{
    private readonly CreateStoreCommandValidator _validator = new();

    [Fact]
    public void Validate_WithInvalidOpeningHoursTimeRange_ReturnsError()
    {
        var command = new CreateStoreCommand(BuildDto());
        command.OpeningHours[0].OpenTime = "18:00";
        command.OpeningHours[0].CloseTime = "07:00";

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("earlier than CloseTime"));
    }

    [Fact]
    public void Validate_WithInvalidCoverImageUrl_ReturnsError()
    {
        var dto = BuildDto();
        dto.CoverImageUrl = "not-a-url";

        var result = _validator.Validate(new CreateStoreCommand(dto));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("CoverImageUrl"));
    }

    [Fact]
    public void Validate_WithDuplicateOpeningDays_ReturnsError()
    {
        var command = new CreateStoreCommand(BuildDto());
        command.OpeningHours[6].DayOfWeek = 5;

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("exactly once"));
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
