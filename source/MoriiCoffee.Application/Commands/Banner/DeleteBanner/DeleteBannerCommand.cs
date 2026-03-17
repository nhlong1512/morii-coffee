using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Banner.DeleteBanner;

/// <summary>Command to soft-delete a banner by ID.</summary>
public class DeleteBannerCommand : ICommand<bool>
{
    public DeleteBannerCommand(Guid id) => Id = id;
    public Guid Id { get; }
}
