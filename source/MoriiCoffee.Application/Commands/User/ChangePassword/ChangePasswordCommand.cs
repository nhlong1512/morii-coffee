using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.User.ChangePassword;

/// <summary>Command to change a user's password. Requires the current password for verification.</summary>
public class ChangePasswordCommand : ICommand<bool>
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
