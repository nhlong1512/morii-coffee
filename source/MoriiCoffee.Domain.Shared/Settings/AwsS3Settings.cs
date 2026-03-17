namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// AWS S3 configuration bound from the <c>AwsS3</c> section in appsettings.json.
/// Two separate IAM credential pairs are used — one for the public (CDN-backed) bucket
/// and one for the private (presigned-URL) bucket — following the principle of least privilege.
/// </summary>
public class AwsS3Settings
{
    public string Region { get; set; } = null!;

    public string PublicBucket { get; set; } = null!;

    public string PrivateBucket { get; set; } = null!;

    public string CdnBaseUrl { get; set; } = null!;

    public string PublicAccessKeyId { get; set; } = null!;

    public string PublicSecretAccessKey { get; set; } = null!;

    public string PrivateAccessKeyId { get; set; } = null!;

    public string PrivateSecretAccessKey { get; set; } = null!;

    public int PresignedUploadExpirySeconds { get; set; } = 300;

    public int PresignedViewExpirySeconds { get; set; } = 900;
}
