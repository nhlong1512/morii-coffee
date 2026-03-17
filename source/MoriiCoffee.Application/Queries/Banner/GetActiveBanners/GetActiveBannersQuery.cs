using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Banner.GetActiveBanners;

/// <summary>Returns all active banners ordered by DisplayOrder for storefront display.</summary>
public record GetActiveBannersQuery : IQuery<List<BannerDto>>;
