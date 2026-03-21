using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Banner.GetAllBanners;

/// <summary>Query to retrieve all non-deleted banners ordered by display order ascending.</summary>
public record GetAllBannersQuery : IQuery<List<BannerDto>>;
