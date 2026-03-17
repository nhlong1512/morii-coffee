using Microsoft.AspNetCore.Http;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Banner.UpdateBanner;

/// <summary>Command to update the fields and/or image of an existing banner.</summary>
public class UpdateBannerCommand : ICommand<BannerDto>
{
    public UpdateBannerCommand(Guid id, UpdateBannerDto dto)
    {
        Id = id;
        Title = dto.Title;
        Description = dto.Description;
        Image = dto.Image;
        DisplayOrder = dto.DisplayOrder;
        IsActive = dto.IsActive;
    }

    public Guid Id { get; }
    public string Title { get; }
    public string? Description { get; }
    public IFormFile? Image { get; }
    public int DisplayOrder { get; }
    public bool IsActive { get; }
}
