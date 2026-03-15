using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Generic file management endpoint.
/// Allows callers to upload or download files directly to/from MinIO without
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

    /// <summary>Upload a file to the specified MinIO bucket.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Upload a file",
        Description = "Uploads a file to the specified MinIO bucket. Returns a presigned URL (valid for 7 days) and the internal object name. Store the object name alongside the URL — it is required for deletion and URL refresh.")]
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
            "POST /api/v1/files/upload — '{FileName}' → bucket '{Bucket}'",
            request.File.FileName, request.BucketName);

        var result = await _fileService.UploadAsync(request.File, request.BucketName);
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Download a file from the specified MinIO bucket.</summary>
    [HttpGet("download")]
    [SwaggerOperation(
        Summary = "Download a file",
        Description = "Streams the file identified by objectName from the given bucket. Returns the raw file bytes with the original content type.")]
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
            "GET /api/v1/files/download — '{Object}' from bucket '{Bucket}'",
            objectName, bucketName);

        var blob = await _fileService.DownloadAsync(bucketName, objectName);

        if (blob?.Content == null)
            return NotFound(new ApiNotFoundResponse($"File '{objectName}' was not found in bucket '{bucketName}'."));

        return File(blob.Content, blob.ContentType ?? "application/octet-stream", objectName);
    }
}
