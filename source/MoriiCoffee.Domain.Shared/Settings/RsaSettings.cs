namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>Holds the RSA-2048 private key (PKCS#8 PEM) used to decrypt passwords encrypted by the frontend.</summary>
public class RsaSettings
{
    public const string SectionName = nameof(RsaSettings);

    /// <summary>PKCS#8 PEM-encoded RSA private key. Newlines may be stored as literal \n in JSON.</summary>
    public string PrivateKey { get; init; } = string.Empty;
}
