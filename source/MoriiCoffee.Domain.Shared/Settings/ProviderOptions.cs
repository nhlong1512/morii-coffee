namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>OAuth provider configuration (e.g., Google, Facebook).</summary>
public class ProviderOptions
{
    /// <summary>OAuth client secret from provider console.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>OAuth client ID from provider console.</summary>
    public string ClientId { get; set; } = string.Empty;
}
