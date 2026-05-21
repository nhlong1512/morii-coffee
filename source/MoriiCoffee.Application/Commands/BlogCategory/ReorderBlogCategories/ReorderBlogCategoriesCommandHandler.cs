using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.BlogCategory.ReorderBlogCategories;

/// <summary>
/// Handles batch updates to blog-category display order.
/// </summary>
public class ReorderBlogCategoriesCommandHandler : ICommandHandler<ReorderBlogCategoriesCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReorderBlogCategoriesCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ReorderBlogCategoriesCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items)
        {
            var category = await _unitOfWork.BlogCategories.GetByIdAsync(item.Id)
                ?? throw new NotFoundException("BlogCategory", item.Id);

            category.DisplayOrder = item.DisplayOrder;
            await _unitOfWork.BlogCategories.Update(category);
        }

        await _unitOfWork.CommitAsync();
        return true;
    }
}
