using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Banner.ToggleBannerStatus;

/// <summary>Command to toggle a banner's active/inactive status.</summary>
public class ToggleBannerStatusCommand : ICommand<BannerDto>
{
    public ToggleBannerStatusCommand(Guid id) => Id = id;
    public Guid Id { get; }
}
