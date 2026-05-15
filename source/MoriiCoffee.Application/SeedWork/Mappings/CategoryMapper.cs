using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.SeedWork.Mappings;

public class CategoryMapper : Profile
{
    public CategoryMapper(AwsS3Settings s3Settings)
    {
        var cdn = s3Settings.CdnBaseUrl;

        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.MapFrom(src => CdnUrlHelper.Resolve(src.IconUrl, cdn)));
    }
}
