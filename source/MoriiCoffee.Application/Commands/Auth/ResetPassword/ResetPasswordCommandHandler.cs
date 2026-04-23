using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ResetPassword;

/// <summary>
/// Resets the user's password by consuming an opaque one-time Redis ticket.
/// The ticket wraps the ASP.NET Identity reset token and the target user ID so the
/// client never sees the raw token. A second attempt with the same ticket is rejected
/// because the ticket is deleted on first successful use.
/// </summary>
public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, bool>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IPasswordResetTicketStore _ticketStore;

    public ResetPasswordCommandHandler(
        UserManager<UserEntity> userManager,
        IPasswordResetTicketStore ticketStore)
    {
        _userManager = userManager;
        _ticketStore = ticketStore;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketStore.GetAndConsumeTicketAsync(request.Ticket);
        if (ticket is null)
            throw new BadRequestException("Reset ticket is invalid, expired, or already used.");

        var user = await _userManager.FindByIdAsync(ticket.UserId.ToString())
            ?? throw new NotFoundException("User", ticket.UserId);

        var result = await _userManager.ResetPasswordAsync(user, ticket.IdentityToken, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"Password reset failed: {errors}");
        }

        return true;
    }
}
