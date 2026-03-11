using Microsoft.AspNetCore.Http;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>Service for uploading and deleting files in cloud object storage (MinIO).</summary>
public interface IFileService
{
    /// <summary>Uploads the given file to the specified MinIO container and returns the public URL.</summary>
    Task<string> UploadAsync(IFormFile file, string container);

    /// <summary>Deletes the file identified by fileName from the specified MinIO container.</summary>
    Task DeleteAsync(string fileName, string container);
}
