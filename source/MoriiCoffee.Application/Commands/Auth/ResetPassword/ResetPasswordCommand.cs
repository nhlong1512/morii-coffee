using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.ResetPassword;

/// <summary>Command to reset a user's password using the token received via email.</summary>
public class ResetPasswordCommand : ICommand<bool>
{
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
