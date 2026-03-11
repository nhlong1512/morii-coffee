using AutoMapper;
using MoriiCoffee.Application.Commands.Auth.SignUp;
using MoriiCoffee.Application.Commands.User.UpdateProfile;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.Aggregates.UserAggregate;

namespace MoriiCoffee.Application.SeedWork.Mappings;

/// <summary>AutoMapper profile for User aggregate mappings. Note: UserDto.Roles is always ignored here and populated manually via UserManager.GetRolesAsync.</summary>
public class UserMapper : Profile
{
    public UserMapper()
    {
        // User → UserDto — Roles must be populated manually in handlers via UserManager.GetRolesAsync
        CreateMap<User, UserDto>()
            .ForMember(d => d.Roles, opt => opt.Ignore());

        // User → UserSummaryDto
        CreateMap<User, UserSummaryDto>();

        // SignUpDto → SignUpCommand
        CreateMap<SignUpDto, SignUpCommand>();

        // UpdateProfileDto → UpdateProfileCommand (UserId set manually in controller)
        CreateMap<UpdateProfileDto, UpdateProfileCommand>()
            .ForMember(d => d.UserId, opt => opt.Ignore());
    }
}
