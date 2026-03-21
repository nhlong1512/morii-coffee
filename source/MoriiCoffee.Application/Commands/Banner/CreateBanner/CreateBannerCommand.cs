using Microsoft.AspNetCore.Http;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Banner.CreateBanner;

/// <summary>Command to create a new promotional banner. An optional image file can be uploaded in the same request.</summary>
public class CreateBannerCommand : ICommand<BannerDto>
{
    public CreateBannerCommand(CreateBannerDto dto)
    {
        Title = dto.Title;
        Subtitle = dto.Subtitle;
        Cta = dto.Cta;
        CtaLink = dto.CtaLink;
        DisplayOrder = dto.DisplayOrder;
        StartDate = dto.StartDate;
        EndDate = dto.EndDate;
        IsActive = dto.IsActive;
        Image = dto.Image;
    }

    /// <summary>Main headline text.</summary>
    public string Title { get; }

    /// <summary>Optional supporting subtitle.</summary>
    public string? Subtitle { get; }

    /// <summary>Call-to-action label.</summary>
    public string? Cta { get; }

    /// <summary>Call-to-action destination URL.</summary>
    public string? CtaLink { get; }

    /// <summary>Sort order (ascending).</summary>
    public int DisplayOrder { get; }

    /// <summary>Optional visibility start date (UTC).</summary>
    public DateTime? StartDate { get; }

    /// <summary>Optional visibility end date (UTC).</summary>
    public DateTime? EndDate { get; }

    /// <summary>Whether the banner is immediately active.</summary>
    public bool IsActive { get; }

    /// <summary>Optional banner image to upload to S3.</summary>
    public IFormFile? Image { get; }
}
