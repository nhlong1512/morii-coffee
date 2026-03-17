using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Banner.ReorderBanners;

/// <summary>Command to update the display order of multiple banners in a single request.</summary>
public class ReorderBannersCommand : ICommand<bool>
{
    public ReorderBannersCommand(List<ReorderBannerItemDto> items) => Items = items;
    public List<ReorderBannerItemDto> Items { get; }
}
