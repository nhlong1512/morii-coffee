using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Constants;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.User.ChangeAvatar;

/// <summary>Uploads the new avatar to S3, updates avatarUrl in the database. Previous S3 files are intentionally left as orphans to keep the flow simple.</summary>
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

        var objectName = S3KeyHelper.BuildS3Key(request.UserId, request.Avatar.FileName);
        var result = await _fileService.UploadAsync(request.Avatar, FileContainers.USERS, objectName);

        user.SetAvatar(result.Blob.Uri, result.Blob.Name);
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var dto = _mapper.Map<UserDto>(user);
        dto.Roles = roles.ToList();
        return dto;
    }
}
