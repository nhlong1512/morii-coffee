using System.Text.Json;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Settings;
using StackExchange.Redis;

namespace MoriiCoffee.Infrastructure.Services.Redis;

/// <summary>
/// Redis-backed store for opaque, one-time password reset tickets.
/// Key pattern: <c>pwdreset:{ticketId}</c> for the ticket payload.
/// A secondary key <c>pwdreset:user:{userId}</c> maps user → active ticket so a new
/// request supersedes any existing one (old ticket is deleted before writing the new one).
/// </summary>
public class RedisPasswordResetTicketStore : IPasswordResetTicketStore
{
    private readonly IDatabase _db;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisPasswordResetTicketStore> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisPasswordResetTicketStore(
        IDatabase db,
        RedisSettings settings,
        ILogger<RedisPasswordResetTicketStore> logger)
    {
        _db = db;
        _settings = settings;
        _logger = logger;
    }

    private static string TicketKey(string ticketId) => $"pwdreset:{ticketId}";
    private static string UserTicketKey(Guid userId) => $"pwdreset:user:{userId}";

    /// <inheritdoc/>
    public async Task<string> CreateTicketAsync(Guid userId, string email, string identityToken)
    {
        try
        {
            var ttl = TimeSpan.FromSeconds(_settings.PasswordResetTicketTtlSeconds);

            // Supersede any existing ticket for this user
            var userKey = UserTicketKey(userId);
            var existingTicketId = await _db.StringGetAsync(userKey);
            if (!existingTicketId.IsNullOrEmpty)
                await _db.KeyDeleteAsync(TicketKey(existingTicketId!));

            var ticketId = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            var ticket = new PasswordResetTicketDto
            {
                TicketId = ticketId,
                UserId = userId,
                Email = email,
                IdentityToken = identityToken,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(ttl)
            };

            var json = JsonSerializer.Serialize(ticket, SerializerOptions);
            await _db.StringSetAsync(TicketKey(ticketId), json, ttl);
            await _db.StringSetAsync(userKey, ticketId, ttl);

            _logger.LogInformation("[TicketStore] Password reset ticket created for user {UserId}.", userId);
            return ticketId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TicketStore] CreateTicketAsync failed for user {UserId}.", userId);
            throw new ServiceUnavailableException("Reset-session storage is unavailable. Please try again later.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<PasswordResetTicketDto?> GetAndConsumeTicketAsync(string ticketId)
    {
        try
        {
            var key = TicketKey(ticketId);
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                _logger.LogWarning("[TicketStore] Ticket '{TicketId}' not found or already consumed.", ticketId);
                return null;
            }

            var ticket = JsonSerializer.Deserialize<PasswordResetTicketDto>((string)value!, SerializerOptions);
            if (ticket is null)
                return null;

            // Atomically consume: remove ticket key and the user→ticket mapping
            await _db.KeyDeleteAsync(key);
            await _db.KeyDeleteAsync(UserTicketKey(ticket.UserId));

            _logger.LogInformation("[TicketStore] Ticket '{TicketId}' consumed for user {UserId}.", ticketId, ticket.UserId);
            return ticket;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TicketStore] GetAndConsumeTicketAsync failed for ticket '{TicketId}'.", ticketId);
            throw new ServiceUnavailableException("Reset-session storage is unavailable. Please try again later.", ex);
        }
    }
}
