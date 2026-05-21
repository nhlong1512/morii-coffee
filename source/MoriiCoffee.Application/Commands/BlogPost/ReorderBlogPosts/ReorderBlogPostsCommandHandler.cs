using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.BlogPost.ReorderBlogPosts;

/// <summary>
/// Handles batch updates to blog post display order.
/// </summary>
public class ReorderBlogPostsCommandHandler : ICommandHandler<ReorderBlogPostsCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReorderBlogPostsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ReorderBlogPostsCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items)
        {
            var post = await _unitOfWork.BlogPosts.GetByIdAsync(item.Id)
                ?? throw new NotFoundException("BlogPost", item.Id);

            post.DisplayOrder = item.DisplayOrder;
            await _unitOfWork.BlogPosts.Update(post);
        }

        await _unitOfWork.CommitAsync();
        return true;
    }
}
