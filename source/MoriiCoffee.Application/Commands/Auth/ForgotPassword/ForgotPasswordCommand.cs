using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.ForgotPassword;

/// <summary>Command to send a password reset email to the specified address.</summary>
public class ForgotPasswordCommand : ICommand<bool>
{
    public string Email { get; set; } = null!;
}
