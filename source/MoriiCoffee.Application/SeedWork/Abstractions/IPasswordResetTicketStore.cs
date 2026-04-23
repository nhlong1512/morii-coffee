using MoriiCoffee.Application.SeedWork.DTOs.Auth;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Redis-backed store for opaque, one-time password reset tickets.
/// The ticket ID sent to the client never exposes the underlying Identity reset token.
/// Creating a new ticket for the same user supersedes any previous one.
/// </summary>
public interface IPasswordResetTicketStore
{
    /// <summary>
    /// Creates an opaque ticket that wraps <paramref name="identityToken"/> and returns the opaque ticket ID.
    /// The ticket expires after the configured TTL.
    /// </summary>
    Task<string> CreateTicketAsync(Guid userId, string email, string identityToken);

    /// <summary>
    /// Retrieves and atomically consumes (deletes) the ticket.
    /// Returns null if the ticket is missing, expired, or already consumed.
    /// </summary>
    Task<PasswordResetTicketDto?> GetAndConsumeTicketAsync(string ticketId);
}
