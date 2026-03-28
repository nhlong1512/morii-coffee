namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>External authentication provider settings.</summary>
public class Authentication
{
    /// <summary>Google OAuth configuration.</summary>
    public ProviderOptions Google { get; set; } = new();

    /// <summary>Facebook OAuth configuration.</summary>
    public ProviderOptions Facebook { get; set; } = new();
}
