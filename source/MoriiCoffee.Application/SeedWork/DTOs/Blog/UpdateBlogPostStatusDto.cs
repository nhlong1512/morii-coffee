using MoriiCoffee.Domain.Shared.Enums.Blog;
using System.ComponentModel.DataAnnotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Blog;

/// <summary>
/// JSON payload for changing a blog post status.
/// </summary>
public class UpdateBlogPostStatusDto
{
    [Required]
    public EBlogPostStatus Status { get; set; }
}
