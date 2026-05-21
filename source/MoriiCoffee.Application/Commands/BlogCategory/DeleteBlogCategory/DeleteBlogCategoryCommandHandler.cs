using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.BlogCategory.DeleteBlogCategory;

/// <summary>
/// Handles safe blog-category deletion by blocking removal while linked posts still exist.
/// </summary>
public class DeleteBlogCategoryCommandHandler : ICommandHandler<DeleteBlogCategoryCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBlogCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteBlogCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.BlogCategories.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("BlogCategory", request.Id);

        if (await _unitOfWork.BlogCategories.CountPostsUsingCategoryAsync(request.Id) > 0)
            throw new BadRequestException("This blog category cannot be deleted because it is still linked to one or more blog posts.");

        await _unitOfWork.BlogCategories.SoftDelete(category);
        await _unitOfWork.CommitAsync();

        return true;
    }
}
