using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.User;

namespace MoriiCoffee.Application.Commands.User.UpdateProfile;

/// <summary>Command to update the display profile fields (name, date of birth, gender, bio) for a given user.</summary>
public class UpdateProfileCommand : ICommand<UserDto>
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public DateTime? Dob { get; set; }
    public EGender? Gender { get; set; }
    public string? Bio { get; set; }
}
