using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Blog;

namespace MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPostStatus;

/// <summary>
/// Command to change the status of a blog post.
/// </summary>
public class UpdateBlogPostStatusCommand : ICommand<BlogPostDetailDto>
{
    public UpdateBlogPostStatusCommand(Guid id, UpdateBlogPostStatusDto dto)
    {
        Id = id;
        Status = dto.Status;
    }

    public Guid Id { get; }
    public EBlogPostStatus Status { get; }
}
