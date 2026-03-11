using Microsoft.AspNetCore.Http;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.User.ChangeAvatar;

/// <summary>Command to replace a user's avatar image. The old file is deleted from storage before the new one is uploaded.</summary>
public class ChangeAvatarCommand : ICommand<UserDto>
{
    public Guid UserId { get; set; }
    public IFormFile Avatar { get; set; } = null!;
}
