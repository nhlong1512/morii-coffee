using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.SignUp;

/// <summary>Command to register a new customer account and return JWT tokens.</summary>
public class SignUpCommand : ICommand<AuthResponseDto>
{
    public SignUpCommand(SignUpDto dto)
        => (Email, PhoneNumber, Password, UserName)
            = (dto.Email, dto.PhoneNumber, dto.Password, dto.UserName);
            
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? UserName { get; set; }
}
