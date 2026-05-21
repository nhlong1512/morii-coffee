using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.BlogCategory.GetPublicBlogCategories;

/// <summary>
/// Returns ordered blog categories for public navigation surfaces.
/// </summary>
public class GetPublicBlogCategoriesQueryHandler : IQueryHandler<GetPublicBlogCategoriesQuery, List<BlogCategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublicBlogCategoriesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<List<BlogCategoryDto>> Handle(GetPublicBlogCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.BlogCategories.FindAll(false);

        if (request.ActiveOnly)
            query = query.Where(x => x.IsActive);

        var categories = query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .AsEnumerable()
            .Select(x => _mapper.Map<BlogCategoryDto>(x))
            .ToList();

        return Task.FromResult(categories);
    }
}
