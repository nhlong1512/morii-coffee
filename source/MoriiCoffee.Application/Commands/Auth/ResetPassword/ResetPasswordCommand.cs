using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.ResetPassword;

/// <summary>
/// Command to reset a user's password using an opaque one-time Redis ticket.
/// <c>Email</c> is optional and retained for backward compatibility only — the handler resolves
/// the account from the ticket, not from the email.
/// </summary>
public class ResetPasswordCommand : ICommand<bool>
{
    /// <summary>Optional email field retained for backward compatibility. Ignored by the handler.</summary>
    public string? Email { get; set; }

    /// <summary>Opaque one-time reset ticket from the password reset email link.</summary>
    public string Ticket { get; set; } = null!;

    /// <summary>New password to set on the account.</summary>
    public string NewPassword { get; set; } = null!;
}
