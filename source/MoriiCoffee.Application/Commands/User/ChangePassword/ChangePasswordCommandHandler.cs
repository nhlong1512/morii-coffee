using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.User.ChangePassword;

/// <summary>Changes the user's password: decrypts both RSA-encrypted passwords, then delegates to UserManager which validates the current password before hashing the new one.</summary>
public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, bool>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IRsaDecryptionService _rsaDecryption;

    public ChangePasswordCommandHandler(
        UserManager<UserEntity> userManager,
        IRsaDecryptionService rsaDecryption)
    {
        _userManager = userManager;
        _rsaDecryption = rsaDecryption;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException("User", request.UserId);

        var plainCurrentPassword = _rsaDecryption.Decrypt(request.CurrentPassword);
        var plainNewPassword = _rsaDecryption.Decrypt(request.NewPassword);
        var result = await _userManager.ChangePasswordAsync(user, plainCurrentPassword, plainNewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"Password change failed: {errors}");
        }

        return true;
    }
}
