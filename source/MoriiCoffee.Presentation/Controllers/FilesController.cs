using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Generic file management endpoint.
/// Allows callers to upload, download, or obtain presigned upload URLs directly without
/// going through a domain-specific command handler.
/// Note: <see cref="IFileService"/> is injected directly — file operations
/// are infrastructure concerns, not domain logic, so MediatR is not used here.
/// </summary>
[ApiController]
[Route("api/v1/files")]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>Upload a file to the specified container (server-side, public bucket only).</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Upload a file",
        Description = "Uploads a file to the specified container. Returns a CDN URL and the internal object name. Store the object name alongside the URL — it is required for deletion and URL refresh.")]
    [SwaggerResponse(200, "File uploaded successfully.", typeof(BlobResponseDto))]
    [SwaggerResponse(400, "No file provided or bucket name is missing.")]
    [SwaggerResponse(500, "Upload failed.")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileDto request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new ApiBadRequestResponse("A file must be provided."));

        if (string.IsNullOrWhiteSpace(request.BucketName))
            return BadRequest(new ApiBadRequestResponse("BucketName is required."));

        _logger.LogInformation(
            "POST /api/v1/files/upload — '{FileName}' → container '{Bucket}'",
            request.File.FileName, request.BucketName);

        var result = await _fileService.UploadAsync(request.File, request.BucketName);
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a presigned PUT URL for direct browser-to-S3 upload (private containers only).</summary>
    [HttpGet("presigned-upload")]
    [SwaggerOperation(
        Summary = "Get a presigned upload URL",
        Description = "Returns a presigned PUT URL (valid 5 min) and a server-generated objectName for direct client-to-S3 upload. Only private containers (e.g. 'users') are supported — use POST /upload for public containers. Store the returned objectName for future deletion and URL refresh.")]
    [SwaggerResponse(200, "Presigned URL generated.", typeof(PresignedUploadResponseDto))]
    [SwaggerResponse(400, "bucketName is missing or refers to a public container.")]
    [SwaggerResponse(500, "Failed to generate presigned URL.")]
    public async Task<IActionResult> GetPresignedUploadUrl([FromQuery] string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            return BadRequest(new ApiBadRequestResponse("bucketName is required."));

        if (FileContainers.IsPublicContainer(bucketName))
            return BadRequest(new ApiBadRequestResponse(
                $"'{bucketName}' is a public container. Upload public assets via POST /api/v1/files/upload."));

        _logger.LogInformation(
            "GET /api/v1/files/presigned-upload — generating PUT URL for container '{Bucket}'",
            bucketName);

        var (presignedUrl, objectName) = await _fileService.GetPresignedUploadUrlAsync(bucketName);

        return Ok(new ApiOkResponse(new PresignedUploadResponseDto
        {
            PresignedUrl = presignedUrl,
            ObjectName = objectName,
            ExpirySeconds = 300
        }));
    }

    /// <summary>Download a file from the specified container.</summary>
    [HttpGet("download")]
    [SwaggerOperation(
        Summary = "Download a file",
        Description = "Streams the file identified by objectName from the given container. Returns the raw file bytes with the original content type.")]
    [SwaggerResponse(200, "File content.")]
    [SwaggerResponse(404, "File not found.")]
    [SwaggerResponse(500, "Download failed.")]
    public async Task<IActionResult> DownloadFile(
        [FromQuery] string bucketName,
        [FromQuery] string objectName)
    {
        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName))
            return BadRequest(new ApiBadRequestResponse("bucketName and objectName are required."));

        _logger.LogInformation(
            "GET /api/v1/files/download — '{Object}' from container '{Bucket}'",
            objectName, bucketName);

        var blob = await _fileService.DownloadAsync(bucketName, objectName);

        if (blob?.Content == null)
            return NotFound(new ApiNotFoundResponse($"File '{objectName}' was not found in bucket '{bucketName}'."));

        return File(blob.Content, blob.ContentType ?? "application/octet-stream", objectName);
    }
}
