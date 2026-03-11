using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// Stub file service that logs instead of interacting with MinIO.
/// Replace with MinIO implementation in a future phase.
/// </summary>
public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadAsync(IFormFile file, string container)
    {
        _logger.LogInformation("[FileService] Uploading file {FileName} to container {Container}",
            file.FileName, container);

        // Return a placeholder URL until MinIO is wired up
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        return Task.FromResult($"https://storage.moriicoffee.com/{container}/{fileName}");
    }

    public Task DeleteAsync(string fileName, string container)
    {
        _logger.LogInformation("[FileService] Deleting file {FileName} from container {Container}",
            fileName, container);
        return Task.CompletedTask;
    }
}
