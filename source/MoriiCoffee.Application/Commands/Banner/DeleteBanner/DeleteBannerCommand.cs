using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Banner.DeleteBanner;

/// <summary>Command to soft-delete a banner by its ID.</summary>
public record DeleteBannerCommand(Guid Id) : ICommand<bool>;
