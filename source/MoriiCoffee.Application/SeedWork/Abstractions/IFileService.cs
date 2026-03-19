using MoriiCoffee.Application.SeedWork.DTOs.File;
using Microsoft.AspNetCore.Http;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Defines the contract for file management services, including operations for uploading, downloading, and deleting files.
/// </summary>
/// <remarks>
/// This interface provides methods for interacting with a file storage system, including obtaining file URIs, listing files, and managing file operations.
/// It is designed to work with various file storage solutions and supports operations for different containers and files.
/// </remarks>
public interface IFileService
{
    /// <summary>
    /// Retrieves the URI of a file in the specified bucketName.
    /// </summary>
    /// <param name="bucketName">The name of the bucketName where the file is stored.</param>
    /// <param name="objectName">The name of the file for which to retrieve the URI.</param>
    /// <param name="storedPolicyName">An optional name of a stored access policy for the bucketName.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the URI of the file.</returns>
    /// <remarks>
    /// If a stored policy name is provided, the URI will be generated based on the policy.
    /// </remarks>
    Task<Uri> GetUriByFileNameAsync(string bucketName, string objectName);

    /// <summary>
    /// Uploads a file to the specified bucketName.
    /// </summary>
    /// <param name="blob">The file to upload.</param>
    /// <param name="bucketName">The name of the bucketName to upload the file to.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response details of the uploaded file.</returns>
    /// <remarks>
    /// The file is uploaded to the specified bucketName. The response details include information about the uploaded file.
    /// </remarks>
    Task<BlobResponseDto> UploadAsync(IFormFile blob, string bucketName);

    /// <summary>
    /// Uploads a file to the specified bucket using a caller-supplied object key.
    /// Use this overload when the storage path must follow a specific naming convention
    /// (e.g., <c>products/{productId}/{timestamp}-{filename}</c>).
    /// </summary>
    /// <param name="blob">The file to upload.</param>
    /// <param name="bucketName">Logical container name (e.g., <c>FileContainers.PRODUCTS</c>).</param>
    /// <param name="customObjectName">
    /// The object name / sub-path within the container.
    /// For public containers this becomes part of the CloudFront CDN URL.
    /// Store this value alongside your entity — it is required for deletion.
    /// </param>
    Task<BlobResponseDto> UploadAsync(IFormFile blob, string bucketName, string customObjectName);

    /// <summary>
    /// Downloads a file from the specified bucketName.
    /// </summary>
    /// <param name="bucketName">The name of the bucketName where the file is stored.</param>
    /// <param name="objectName">The name of the file to download.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file metadata and content if available, or null if not found.</returns>
    /// <remarks>
    /// If the file is found, the metadata and content are returned. If the file does not exist, the result will be null.
    /// </remarks>
    Task<BlobDto?> DownloadAsync(string bucketName, string objectName);

    /// <summary>
    /// Deletes a file from the specified bucketName.
    /// </summary>
    /// <param name="bucketName">The name of the bucketName where the file is stored.</param>
    /// <param name="objectName">The name of the file to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response details of the deleted file.</returns>
    /// <remarks>
    /// The file is removed from the specified bucketName. The response details include information about the deleted file.
    /// </remarks>
    Task<BlobResponseDto> DeleteAsync(string bucketName, string objectName);

    /// <summary>
    /// Generates a presigned PUT URL for direct client-to-storage upload without routing through the server.
    /// Intended for private containers only — public containers should use <see cref="UploadAsync"/> instead.
    /// </summary>
    /// <param name="bucketName">Logical container name (e.g., <c>FileContainers.USERS</c>).</param>
    /// <param name="expirySeconds">Presigned URL lifetime in seconds (default: 300 = 5 min).</param>
    /// <returns>
    /// A tuple of the presigned PUT URL and the server-generated object name (GUID).
    /// Store the object name alongside your entity — it is required for deletion and URL refresh.
    /// </returns>
    Task<(string presignedUrl, string objectName)> GetPresignedUploadUrlAsync(
        string bucketName,
        int expirySeconds = 300);
}
