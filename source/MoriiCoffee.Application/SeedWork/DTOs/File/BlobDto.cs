using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.File;

public class BlobDto
{
    [SwaggerSchema("URI of the blob resource")]
    public string? Uri { get; set; }

    [SwaggerSchema("Name of the blob file")]
    public string? Name { get; set; }

    [SwaggerSchema("MIME type of the blob content")]
    public string? ContentType { get; set; }

    [SwaggerSchema("Stream representing the content of the blob")]
    public Stream? Content { get; set; }

    [SwaggerSchema("Size of the blob content in bytes")]
    public long Size { get; set; } = 0;

    /// <summary>
    /// Full S3/storage key including the container prefix (e.g. <c>products/uuid/timestamp-file.png</c>).
    /// Store this in the database instead of <see cref="Uri"/> so URLs survive CDN domain changes.
    /// </summary>
    [SwaggerSchema("Storage key (relative path within the bucket, including container prefix)")]
    public string? StorageKey { get; set; }
}
