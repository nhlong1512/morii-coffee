using Microsoft.AspNetCore.Http;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Banner.CreateBanner;

/// <summary>Command to create a new promotional banner.</summary>
public class CreateBannerCommand : ICommand<BannerDto>
{
    public CreateBannerCommand(CreateBannerDto dto)
    {
        Title = dto.Title;
        Description = dto.Description;
        Image = dto.Image;
        DisplayOrder = dto.DisplayOrder;
        IsActive = dto.IsActive;
    }

    public string Title { get; }
    public string? Description { get; }
    public IFormFile? Image { get; }
    public int DisplayOrder { get; }
    public bool IsActive { get; }
}
