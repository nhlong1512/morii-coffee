using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ResetPassword;

/// <summary>Resets the user's password: decrypts the RSA-encrypted new password, then applies it via the Identity token from the forgot-password email.</summary>
public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, bool>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IRsaDecryptionService _rsaDecryption;

    public ResetPasswordCommandHandler(
        UserManager<UserEntity> userManager,
        IRsaDecryptionService rsaDecryption)
    {
        _userManager = userManager;
        _rsaDecryption = rsaDecryption;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new NotFoundException("User", request.Email);

        var plainPassword = _rsaDecryption.Decrypt(request.NewPassword);
        var result = await _userManager.ResetPasswordAsync(user, request.Token, plainPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"Password reset failed: {errors}");
        }

        return true;
    }
}
