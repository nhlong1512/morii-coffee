using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.SeedWork.Mappings;

/// <summary>AutoMapper profile for the Banner aggregate.</summary>
public class BannerMapper : Profile
{
    public BannerMapper(AwsS3Settings s3Settings)
    {
        var cdn = s3Settings.CdnBaseUrl;

        CreateMap<Banner, BannerDto>()
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => CdnUrlHelper.Resolve(src.ImageUrl, cdn)));
    }
}
