using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.BlogPost.DeleteBlogPost;

/// <summary>
/// Command to soft-delete a blog post.
/// </summary>
public record DeleteBlogPostCommand(Guid Id) : ICommand<bool>;
