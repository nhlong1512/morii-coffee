using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Banner.GetBannerById;

/// <summary>Query to retrieve a single banner by its unique identifier.</summary>
public record GetBannerByIdQuery(Guid BannerId) : IQuery<BannerDto>;
