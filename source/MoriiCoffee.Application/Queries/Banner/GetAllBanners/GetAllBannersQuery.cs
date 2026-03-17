using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Banner.GetAllBanners;

/// <summary>Admin query — returns all banners (active and inactive) with pagination.</summary>
public record GetAllBannersQuery(PaginationFilter Filter) : IQuery<Pagination<BannerDto>>;
