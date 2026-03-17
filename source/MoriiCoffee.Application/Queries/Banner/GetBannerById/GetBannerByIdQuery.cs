using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Banner.GetBannerById;

/// <summary>Returns a single banner by its ID.</summary>
public record GetBannerByIdQuery(Guid Id) : IQuery<BannerDto>;
