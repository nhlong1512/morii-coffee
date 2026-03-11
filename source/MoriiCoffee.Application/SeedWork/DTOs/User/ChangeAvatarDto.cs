using Microsoft.AspNetCore.Http;

namespace MoriiCoffee.Application.SeedWork.DTOs.User;

/// <summary>Multipart form-data body for PUT /users/me/avatar. Carries the image file to upload.</summary>
public class ChangeAvatarDto
{
    public IFormFile Avatar { get; set; } = null!;
}
