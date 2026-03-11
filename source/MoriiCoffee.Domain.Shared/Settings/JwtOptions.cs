namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>Configuration options for JWT access token generation and validation. Bound from the JwtOptions config section.</summary>
public class JwtOptions
{
    /// <summary>HMAC-SHA256 signing secret. Must be at least 32 characters long.</summary>
    public string Secret { get; set; } = null!;

    /// <summary>Token issuer claim value (e.g., "MoriiCoffee"). Must match the value configured in TokenValidationParameters.</summary>
    public string Issuer { get; set; } = null!;

    /// <summary>Token audience claim value (e.g., "MoriiCoffee"). Must match the value configured in TokenValidationParameters.</summary>
    public string Audience { get; set; } = null!;

    /// <summary>Lifetime of the JWT access token in minutes (e.g., 15 for 15 minutes).</summary>
    public int AccessTokenExpiryInMinutes { get; set; }

    /// <summary>Lifetime of the opaque refresh token in days (e.g., 7 for one week).</summary>
    public int RefreshTokenExpiryInDays { get; set; }
}
