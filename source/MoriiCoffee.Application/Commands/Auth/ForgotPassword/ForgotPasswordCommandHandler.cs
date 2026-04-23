using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Settings;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ForgotPassword;

/// <summary>
/// Sends a password reset email containing an opaque one-time ticket.
/// Always returns true regardless of whether the account exists to prevent email enumeration.
/// The reset URL carries a ticket ID rather than the raw Identity token so the token is
/// never exposed to the client.
/// Throws ServiceUnavailableException if Redis is unavailable (ticket creation requires Redis).
/// </summary>
public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, bool>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IEmailService _emailService;
    private readonly IPasswordResetTicketStore _ticketStore;
    private readonly EmailSettings _emailSettings;

    public ForgotPasswordCommandHandler(
        UserManager<UserEntity> userManager,
        IEmailService emailService,
        IPasswordResetTicketStore ticketStore,
        EmailSettings emailSettings)
    {
        _userManager = userManager;
        _emailService = emailService;
        _ticketStore = ticketStore;
        _emailSettings = emailSettings;
    }

    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return true; // Anti-enumeration: always respond the same way

        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var ticketId = await _ticketStore.CreateTicketAsync(user.Id, user.Email!, identityToken);

        var resetUrl = $"{_emailSettings.ResetPasswordBaseUrl}?ticket={ticketId}";

        _ = _emailService.SendPasswordResetEmailAsync(user.Email!, resetUrl);

        return true;
    }
}
