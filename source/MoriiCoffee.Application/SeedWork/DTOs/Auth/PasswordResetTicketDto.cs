namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>
/// Redis-stored payload for an opaque one-time password reset ticket.
/// Never exposed directly to the client — only the opaque ticket ID is sent via email.
/// </summary>
public class PasswordResetTicketDto
{
    /// <summary>Opaque identifier sent to the client in the reset URL.</summary>
    public string TicketId { get; set; } = null!;

    /// <summary>Account being recovered.</summary>
    public Guid UserId { get; set; }

    /// <summary>Email address; stored for audit context.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Server-side ASP.NET Identity reset credential; never returned to the client.</summary>
    public string IdentityToken { get; set; } = null!;

    /// <summary>UTC timestamp when the ticket was issued.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Hard expiration; the ticket must be consumed before this time.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}
