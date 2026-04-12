using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Banner.CreateBanner;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Banner;

public class CreateBannerCommandValidatorTests
{
    private readonly CreateBannerCommandValidator _validator = new();

    private static CreateBannerCommand ValidCommand() => new(new CreateBannerDto
    {
        Title = "Summer Promo",
        DisplayOrder = 0,
        IsActive = true
    });

    [Fact]
    public void Validate_EmptyTitle_ReturnsError()
    {
        var cmd = new CreateBannerCommand(new CreateBannerDto { Title = "", DisplayOrder = 0 });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_TitleExceeds200Chars_ReturnsError()
    {
        var cmd = new CreateBannerCommand(new CreateBannerDto { Title = new string('a', 201), DisplayOrder = 0 });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_NegativeDisplayOrder_ReturnsError()
    {
        var cmd = new CreateBannerCommand(new CreateBannerDto { Title = "Promo", DisplayOrder = -1 });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }

    [Fact]
    public void Validate_EndDateBeforeStartDate_ReturnsError()
    {
        var cmd = new CreateBannerCommand(new CreateBannerDto
        {
            Title = "Promo",
            DisplayOrder = 0,
            StartDate = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidCommandWithDates_NoErrors()
    {
        var cmd = new CreateBannerCommand(new CreateBannerDto
        {
            Title = "Promo",
            DisplayOrder = 0,
            StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc)
        });
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
