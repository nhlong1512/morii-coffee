using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// AWS S3–backed implementation of <see cref="IFileService"/> with a two-bucket strategy.
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Public containers</b> (products, categories, banners) are stored in <c>moriicoffee-public</c>
///       under <c>{container}/{guid}</c> and always return a CloudFront CDN URL — never the raw S3 URL.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Private containers</b> (users) are stored in <c>moriicoffee-private</c> under
///       <c>{container}/{guid}</c>. Upload responses return a presigned GET URL valid for
///       <see cref="AwsS3Settings.PresignedViewExpirySeconds"/> seconds (default 15 min).
///     </description>
///   </item>
/// </list>
/// Only the GUID is stored as <c>objectName</c> in the database; the container name is always
/// passed separately so callers never need to reconstruct the full S3 key themselves.
/// </summary>
public class AwsS3FileService : IFileService
{
    private readonly IAmazonS3 _publicS3;
    private readonly IAmazonS3 _privateS3;
    private readonly AwsS3Settings _settings;
    private readonly ILogger<AwsS3FileService> _logger;

    public AwsS3FileService(
        [FromKeyedServices("s3-public")] IAmazonS3 publicS3,
        [FromKeyedServices("s3-private")] IAmazonS3 privateS3,
        AwsS3Settings settings,
        ILogger<AwsS3FileService> logger)
    {
        if (string.IsNullOrWhiteSpace(settings.CdnBaseUrl))
            throw new InvalidOperationException(
                "AwsS3:CdnBaseUrl is required. Public-bucket uploads must return a CDN URL, never a raw S3 URL.");

        _publicS3 = publicS3;
        _privateS3 = privateS3;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Returns the URL for an existing object.
    /// Public objects return a permanent CDN URL; private objects return a fresh presigned GET URL (15 min).
    /// </summary>
    public Task<Uri> GetUriByFileNameAsync(string bucketName, string objectName)
    {
        var s3Key = BuildS3Key(bucketName, objectName);
        var url = FileContainers.IsPublicContainer(bucketName)
            ? BuildCdnUrl(s3Key)
            : BuildPresignedGetUrl(s3Key);

        return Task.FromResult(new Uri(url));
    }

    /// <summary>
    /// Uploads <paramref name="blob"/> to the bucket determined by <paramref name="bucketName"/>.
    /// Public uploads return a permanent CDN URL. Private uploads return a presigned GET URL (15 min).
    /// </summary>
    public async Task<BlobResponseDto> UploadAsync(IFormFile blob, string bucketName)
    {
        bool isPublic = FileContainers.IsPublicContainer(bucketName);
        var s3Client = isPublic ? _publicS3 : _privateS3;
        var actualBucket = isPublic ? _settings.PublicBucket : _settings.PrivateBucket;

        var objectName = Guid.NewGuid().ToString("N");
        var s3Key = BuildS3Key(bucketName, objectName);

        using var stream = blob.OpenReadStream();
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = actualBucket,
            Key = s3Key,
            InputStream = stream,
            ContentType = blob.ContentType ?? "application/octet-stream",
            AutoCloseStream = false
        });

        _logger.LogInformation(
            "[AwsS3FileService] Uploaded '{OriginalName}' → '{Key}' in bucket '{Bucket}'",
            blob.FileName, s3Key, actualBucket);

        var uri = isPublic ? BuildCdnUrl(s3Key) : BuildPresignedGetUrl(s3Key);

        var response = new BlobResponseDto { Status = "File uploaded successfully." };
        response.Blob.Uri = uri;
        response.Blob.Name = objectName;
        response.Blob.ContentType = blob.ContentType;
        response.Blob.Size = blob.Length;
        return response;
    }

    /// <summary>
    /// Uploads <paramref name="blob"/> to the bucket using a caller-supplied <paramref name="customObjectName"/>
    /// instead of an auto-generated GUID. Use this when the storage path must follow a specific naming
    /// convention (e.g., <c>products/{productId}/{timestamp}-{filename}</c>).
    /// </summary>
    public async Task<BlobResponseDto> UploadAsync(IFormFile blob, string bucketName, string customObjectName)
    {
        bool isPublic = FileContainers.IsPublicContainer(bucketName);
        var s3Client = isPublic ? _publicS3 : _privateS3;
        var actualBucket = isPublic ? _settings.PublicBucket : _settings.PrivateBucket;

        var s3Key = BuildS3Key(bucketName, customObjectName);

        using var stream = blob.OpenReadStream();
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = actualBucket,
            Key = s3Key,
            InputStream = stream,
            ContentType = blob.ContentType ?? "application/octet-stream",
            AutoCloseStream = false
        });

        _logger.LogInformation(
            "[AwsS3FileService] Uploaded '{OriginalName}' → '{Key}' in bucket '{Bucket}'",
            blob.FileName, s3Key, actualBucket);

        var uri = isPublic ? BuildCdnUrl(s3Key) : BuildPresignedGetUrl(s3Key);

        var response = new BlobResponseDto { Status = "File uploaded successfully." };
        response.Blob.Uri = uri;
        response.Blob.Name = customObjectName;
        response.Blob.ContentType = blob.ContentType;
        response.Blob.Size = blob.Length;
        return response;
    }

    /// <summary>
    /// Streams the object from the appropriate S3 bucket.
    /// Returns <c>null</c> if the object does not exist.
    /// </summary>
    public async Task<BlobDto?> DownloadAsync(string bucketName, string objectName)
    {
        try
        {
            bool isPublic = FileContainers.IsPublicContainer(bucketName);
            var s3Client = isPublic ? _publicS3 : _privateS3;
            var actualBucket = isPublic ? _settings.PublicBucket : _settings.PrivateBucket;

            var response = await s3Client.GetObjectAsync(actualBucket, BuildS3Key(bucketName, objectName));

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return new BlobDto
            {
                Name = objectName,
                ContentType = response.Headers.ContentType,
                Content = memoryStream,
                Size = response.ContentLength
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex,
                "[AwsS3FileService] Object '{Object}' not found in container '{Container}'",
                objectName, bucketName);
            return null;
        }
    }

    /// <summary>
    /// Removes the object from the appropriate S3 bucket.
    /// Swallows errors so a missing file does not fail the calling business operation.
    /// </summary>
    public async Task<BlobResponseDto> DeleteAsync(string bucketName, string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            var skipped = new BlobResponseDto { Status = "No object name provided — delete skipped." };
            skipped.Blob.Name = objectName;
            return skipped;
        }

        try
        {
            bool isPublic = FileContainers.IsPublicContainer(bucketName);
            var s3Client = isPublic ? _publicS3 : _privateS3;
            var actualBucket = isPublic ? _settings.PublicBucket : _settings.PrivateBucket;
            var s3Key = BuildS3Key(bucketName, objectName);

            await s3Client.DeleteObjectAsync(actualBucket, s3Key);

            _logger.LogInformation("[AwsS3FileService] Deleted '{Key}' from bucket '{Bucket}'", s3Key, actualBucket);

            var success = new BlobResponseDto { Status = "File deleted successfully." };
            success.Blob.Name = objectName;
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[AwsS3FileService] Failed to delete '{Object}' from container '{Container}'",
                objectName, bucketName);

            var failed = new BlobResponseDto { Status = $"Delete failed: {ex.Message}", Error = true };
            failed.Blob.Name = objectName;
            return failed;
        }
    }

    /// <summary>
    /// Generates a presigned PUT URL for direct browser-to-S3 upload to a private container.
    /// Throws <see cref="InvalidOperationException"/> if called for a public container.
    /// </summary>
    public Task<(string presignedUrl, string objectName)> GetPresignedUploadUrlAsync(
        string bucketName,
        int expirySeconds = 300)
    {
        if (FileContainers.IsPublicContainer(bucketName))
            throw new InvalidOperationException(
                $"Container '{bucketName}' is public. Use UploadAsync for server-side upload.");

        var objectName = Guid.NewGuid().ToString("N");
        var s3Key = BuildS3Key(bucketName, objectName);

        var presignedUrl = _privateS3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _settings.PrivateBucket,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(expirySeconds)
        });

        return Task.FromResult((presignedUrl, objectName));
    }

    private static string BuildS3Key(string container, string objectName) =>
        $"{container}/{objectName}";

    private string BuildCdnUrl(string s3Key) =>
        $"{_settings.CdnBaseUrl.TrimEnd('/')}/{s3Key}";

    private string BuildPresignedGetUrl(string s3Key) =>
        _privateS3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _settings.PrivateBucket,
            Key = s3Key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddSeconds(_settings.PresignedViewExpirySeconds)
        });
}
