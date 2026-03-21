using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;

namespace MoriiCoffee.Application.SeedWork.Mappings;

/// <summary>AutoMapper profile for the Banner aggregate.</summary>
public class BannerMapper : Profile
{
    public BannerMapper()
    {
        CreateMap<Banner, BannerDto>();
    }
}
