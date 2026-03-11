using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.RefreshToken;

/// <summary>Command to exchange an expired JWT and a valid refresh token for a new token pair.</summary>
public class RefreshTokenCommand : ICommand<AuthResponseDto>
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}
