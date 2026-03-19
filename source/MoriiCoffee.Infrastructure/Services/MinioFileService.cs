using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// MinIO-backed implementation of <see cref="IFileService"/>.
/// Each file is stored under a random GUID object name (no original filename leakage).
/// Buckets are auto-created on first upload if they do not yet exist.
/// Presigned GET URLs are valid for <see cref="MinioSettings.PresignedUrlExpirySeconds"/> seconds.
/// </summary>
public class MinioFileService : IFileService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioFileService> _logger;

    public MinioFileService(
        IMinioClient minioClient,
        MinioSettings settings,
        ILogger<MinioFileService> logger)
    {
        _minioClient = minioClient;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Generates a presigned GET URL for an existing object in <paramref name="bucketName"/>.
    /// Useful for refreshing expired URLs stored in the database.
    /// </summary>
    public async Task<Uri> GetUriByFileNameAsync(string bucketName, string objectName)
    {
        string presignedUrl = await _minioClient.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(_settings.PresignedUrlExpirySeconds));

        return new Uri(presignedUrl);
    }

    /// <summary>
    /// Uploads <paramref name="blob"/> to <paramref name="bucketName"/>.
    /// The bucket is auto-created when it does not exist.
    /// Returns a <see cref="BlobResponseDto"/> with the presigned URL and internal object name.
    /// </summary>
    public async Task<BlobResponseDto> UploadAsync(IFormFile blob, string bucketName)
    {
        bucketName = bucketName.ToLowerInvariant();

        // Auto-create the bucket if it does not exist
        bool bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName));

        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName));

            _logger.LogInformation("[MinioFileService] Created bucket '{Bucket}'", bucketName);
        }

        // Use a GUID as the object name — never expose the original client filename
        string objectName = Guid.NewGuid().ToString("N");

        using (var stream = blob.OpenReadStream())
        {
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(blob.Length)
                .WithContentType(blob.ContentType ?? "application/octet-stream"));
        }

        _logger.LogInformation(
            "[MinioFileService] Uploaded '{OriginalName}' → '{Object}' in bucket '{Bucket}'",
            blob.FileName, objectName, bucketName);

        Uri presignedUri = await GetUriByFileNameAsync(bucketName, objectName);

        var response = new BlobResponseDto
        {
            Status = "File uploaded successfully."
        };
        response.Blob.Uri = presignedUri.ToString();
        response.Blob.Name = objectName;
        response.Blob.ContentType = blob.ContentType;
        response.Blob.Size = blob.Length;

        return response;
    }

    /// <summary>
    /// Uploads <paramref name="blob"/> to <paramref name="bucketName"/> using a caller-supplied
    /// <paramref name="customObjectName"/> instead of an auto-generated GUID.
    /// Use this when the storage path must follow a specific naming convention
    /// (e.g., <c>products/{productId}/{timestamp}-{filename}</c>).
    /// </summary>
    public async Task<BlobResponseDto> UploadAsync(IFormFile blob, string bucketName, string customObjectName)
    {
        bucketName = bucketName.ToLowerInvariant();

        bool bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName));

        if (!bucketExists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName));

            _logger.LogInformation("[MinioFileService] Created bucket '{Bucket}'", bucketName);
        }

        using (var stream = blob.OpenReadStream())
        {
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(customObjectName)
                .WithStreamData(stream)
                .WithObjectSize(blob.Length)
                .WithContentType(blob.ContentType ?? "application/octet-stream"));
        }

        _logger.LogInformation(
            "[MinioFileService] Uploaded '{OriginalName}' → '{Object}' in bucket '{Bucket}'",
            blob.FileName, customObjectName, bucketName);

        Uri presignedUri = await GetUriByFileNameAsync(bucketName, customObjectName);

        var response = new BlobResponseDto { Status = "File uploaded successfully." };
        response.Blob.Uri = presignedUri.ToString();
        response.Blob.Name = customObjectName;
        response.Blob.ContentType = blob.ContentType;
        response.Blob.Size = blob.Length;
        return response;
    }

    /// <summary>
    /// Streams the object identified by <paramref name="objectName"/> from <paramref name="bucketName"/>.
    /// Returns null if the object does not exist.
    /// </summary>
    public async Task<BlobDto?> DownloadAsync(string bucketName, string objectName)
    {
        try
        {
            var memoryStream = new MemoryStream();

            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream((stream, ct) =>
                {
                    stream.CopyTo(memoryStream);
                    return Task.CompletedTask;
                }));

            var stat = await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName));

            memoryStream.Position = 0;

            return new BlobDto
            {
                Name = objectName,
                ContentType = stat.ContentType,
                Content = memoryStream,
                Size = stat.Size
            };
        }
        catch (MinioException ex)
        {
            _logger.LogWarning(ex,
                "[MinioFileService] Object '{Object}' not found in bucket '{Bucket}'",
                objectName, bucketName);
            return null;
        }
    }

    /// <summary>
    /// Generates a presigned PUT URL for direct client-to-MinIO upload.
    /// A new GUID object name is generated server-side and returned alongside the URL.
    /// </summary>
    public async Task<(string presignedUrl, string objectName)> GetPresignedUploadUrlAsync(
        string bucketName,
        int expirySeconds = 300)
    {
        var objectName = Guid.NewGuid().ToString("N");

        string presignedUrl = await _minioClient.PresignedPutObjectAsync(
            new PresignedPutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expirySeconds));

        return (presignedUrl, objectName);
    }

    /// <summary>
    /// Removes the object identified by <paramref name="objectName"/> from <paramref name="bucketName"/>.
    /// Swallows errors so a missing file does not fail the business operation.
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
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName));

            _logger.LogInformation(
                "[MinioFileService] Deleted '{Object}' from bucket '{Bucket}'",
                objectName, bucketName);

            var success = new BlobResponseDto { Status = "File deleted successfully." };
            success.Blob.Name = objectName;
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[MinioFileService] Failed to delete '{Object}' from bucket '{Bucket}'",
                objectName, bucketName);

            var failed = new BlobResponseDto
            {
                Status = $"Delete failed: {ex.Message}",
                Error = true
            };
            failed.Blob.Name = objectName;
            return failed;
        }
    }
}
