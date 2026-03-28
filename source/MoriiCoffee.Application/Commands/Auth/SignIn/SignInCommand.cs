using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.SignIn;

/// <summary>Command to authenticate a user by email address and return JWT tokens.</summary>
public class SignInCommand : ICommand<AuthResponseDto>
{
    /// <summary>Email address. Phone numbers are no longer accepted as identity for authentication.</summary>
    public string Identity { get; set; } = null!;

    /// <summary>User password credential.</summary>
    public string Password { get; set; } = null!;
}
