using Microsoft.AspNetCore.Http;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Banner.UpdateBanner;

/// <summary>Command to update the metadata of an existing banner. An optional image file can be uploaded in the same request.</summary>
public class UpdateBannerCommand : ICommand<BannerDto>
{
    public UpdateBannerCommand(Guid id, UpdateBannerDto dto)
    {
        Id = id;
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

    /// <summary>Identifier of the banner to update.</summary>
    public Guid Id { get; }

    /// <summary>Updated headline text.</summary>
    public string Title { get; }

    /// <summary>Updated subtitle.</summary>
    public string? Subtitle { get; }

    /// <summary>Updated CTA label.</summary>
    public string? Cta { get; }

    /// <summary>Updated CTA URL.</summary>
    public string? CtaLink { get; }

    /// <summary>Updated display order.</summary>
    public int DisplayOrder { get; }

    /// <summary>Updated start date.</summary>
    public DateTime? StartDate { get; }

    /// <summary>Updated end date.</summary>
    public DateTime? EndDate { get; }

    /// <summary>Updated active flag.</summary>
    public bool IsActive { get; }

    /// <summary>Optional replacement image to upload to S3.</summary>
    public IFormFile? Image { get; }
}
