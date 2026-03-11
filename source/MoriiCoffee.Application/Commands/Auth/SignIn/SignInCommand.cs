using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.SignIn;

/// <summary>Command to authenticate a user by email or username and return JWT tokens.</summary>
public class SignInCommand : ICommand<AuthResponseDto>
{
    /// <summary>Email address or username.</summary>
    public string Identity { get; set; } = null!;
    public string Password { get; set; } = null!;
}
