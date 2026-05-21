using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.BlogCategory.GetAdminBlogCategories;

/// <summary>
/// Returns a paginated admin list of blog categories including inactive ones.
/// </summary>
public class GetAdminBlogCategoriesQueryHandler : IQueryHandler<GetAdminBlogCategoriesQuery, Pagination<BlogCategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAdminBlogCategoriesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Pagination<BlogCategoryDto>> Handle(GetAdminBlogCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.BlogCategories.FindAll(false);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                (x.Description != null && x.Description.ToLower().Contains(search)));
        }

        var dtoQuery = query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .AsEnumerable()
            .Select(x => _mapper.Map<BlogCategoryDto>(x))
            .AsQueryable();

        return Task.FromResult(PagingHelper.QueryPaginate(request.Filter, dtoQuery));
    }
}
