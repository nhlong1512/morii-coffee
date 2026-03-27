using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Settings;
using System.Text;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ForgotPassword;

/// <summary>Sends a password reset email if the account exists. Always returns true to avoid email enumeration.</summary>
public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, bool>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IEmailService _emailService;
    private readonly EmailSettings _emailSettings;

    public ForgotPasswordCommandHandler(
        UserManager<UserEntity> userManager,
        IEmailService emailService,
        EmailSettings emailSettings)
    {
        _userManager = userManager;
        _emailService = emailService;
        _emailSettings = emailSettings;
    }

    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) {
            return true;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var emailEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(user.Email!));

        var resetUrl = $"{_emailSettings.ResetPasswordBaseUrl}?token={tokenEncoded}&email={emailEncoded}";

        _ = _emailService.SendPasswordResetEmailAsync(user.Email!, resetUrl);

        return true;
    }
}
