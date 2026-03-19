using Microsoft.AspNetCore.Http;

namespace MoriiCoffee.Application.SeedWork.DTOs.ProductImage;

/// <summary>
/// Multipart/form-data payload for uploading one or more product gallery images.
/// Each file is uploaded to the public S3 bucket and served via CloudFront CDN.
/// </summary>
public class UploadProductImagesDto
{
    /// <summary>
    /// One or more image files to upload.
    /// Supported types: jpg, jpeg, png, webp. Max size per file: 5 MB.
    /// </summary>
    public List<IFormFile> Files { get; set; } = new();
}
