using Microsoft.AspNetCore.Http;

namespace MoriiCoffee.Application.SeedWork.DTOs.File;

public class UploadFileDto
{
    public IFormFile File { get; set; } = null!;

    public string BucketName { get; set; } = null!;
}
