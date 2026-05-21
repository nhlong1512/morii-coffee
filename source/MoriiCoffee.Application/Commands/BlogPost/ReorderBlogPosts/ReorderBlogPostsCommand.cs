using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.BlogPost.ReorderBlogPosts;

/// <summary>
/// Command to reorder multiple blog posts in a single request.
/// </summary>
public class ReorderBlogPostsCommand : ICommand<bool>
{
    public ReorderBlogPostsCommand(ReorderBlogPostsDto dto)
    {
        Items = dto.Items;
    }

    public List<ReorderBlogPostsItemDto> Items { get; }
}
