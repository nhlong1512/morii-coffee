using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Constants;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.User.ChangeAvatar;

/// <summary>Deletes the existing avatar from MinIO (if any), uploads the new file, updates the domain entity via UserManager.</summary>
public class ChangeAvatarCommandHandler : ICommandHandler<ChangeAvatarCommand, UserDto>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public ChangeAvatarCommandHandler(
        UserManager<UserEntity> userManager,
        IFileService fileService,
        IMapper mapper)
    {
        _userManager = userManager;
        _fileService = fileService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(ChangeAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException("User", request.UserId);

        if (!string.IsNullOrEmpty(user.AvatarFileName))
            await _fileService.DeleteAsync(user.AvatarFileName, FileContainers.USERS);

        var url = await _fileService.UploadAsync(request.Avatar, FileContainers.USERS);
        var fileName = Path.GetFileName(url);

        user.SetAvatar(url, fileName);
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var dto = _mapper.Map<UserDto>(user);
        dto.Roles = roles.ToList();
        return dto;
    }
}
