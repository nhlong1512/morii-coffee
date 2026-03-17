using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.File;

/// <summary>
/// Response payload for <c>GET /api/v1/files/presigned-upload</c>.
/// Contains the presigned PUT URL for direct browser-to-S3 upload and the server-generated
/// object name that must be stored by the caller for future deletion and URL refresh.
/// </summary>
public class PresignedUploadResponseDto
{
    /// <summary>Presigned PUT URL for the direct S3 upload (valid for <see cref="ExpirySeconds"/> seconds).</summary>
    [SwaggerSchema("Presigned PUT URL for the direct S3 upload.")]
    public string PresignedUrl { get; set; } = null!;

    /// <summary>
    /// Server-generated GUID identifying the object within its container.
    /// Store this value alongside your entity — it is required for deletion and URL refresh.
    /// </summary>
    [SwaggerSchema("Server-generated object name (GUID). Store this for future deletion and URL refresh.")]
    public string ObjectName { get; set; } = null!;

    /// <summary>Number of seconds until the presigned URL expires.</summary>
    [SwaggerSchema("Presigned URL lifetime in seconds.")]
    public int ExpirySeconds { get; set; }
}
