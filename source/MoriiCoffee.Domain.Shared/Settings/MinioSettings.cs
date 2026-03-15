namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Configuration options for the MinIO object storage client.
/// Bound from the <c>MinioSettings</c> section in appsettings.json.
/// </summary>
public class MinioSettings
{
    public string Endpoint { get; set; } = null!;

    /// <summary>MinIO access key (equivalent to AWS Access Key ID).</summary>
    public string AccessKey { get; set; } = null!;

    /// <summary>MinIO secret key (equivalent to AWS Secret Access Key).</summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>Whether to use HTTPS when connecting to MinIO. Set false for local dev, true for production.</summary>
    public bool WithSSL { get; set; }

    /// <summary>Presigned URL expiry in seconds (default: 604800 = 7 days).</summary>
    public int PresignedUrlExpirySeconds { get; set; } = 7 * 24 * 3600;
}
