using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.BlogPost.DeleteBlogPost;

/// <summary>
/// Handles soft deletion of a blog post.
/// </summary>
public class DeleteBlogPostCommandHandler : ICommandHandler<DeleteBlogPostCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBlogPostCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _unitOfWork.BlogPosts.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("BlogPost", request.Id);

        await _unitOfWork.BlogPosts.SoftDelete(post);
        await _unitOfWork.CommitAsync();

        return true;
    }
}
