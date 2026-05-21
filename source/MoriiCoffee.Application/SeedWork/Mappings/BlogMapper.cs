using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.SeedWork.Mappings;

/// <summary>
/// AutoMapper profile for blog posts and blog categories.
/// </summary>
public class BlogMapper : Profile
{
    public BlogMapper(AwsS3Settings s3Settings)
    {
        var cdn = s3Settings.CdnBaseUrl;

        CreateMap<BlogCategory, BlogCategoryDto>();

        CreateMap<BlogPost, BlogPostSummaryDto>()
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => CdnUrlHelper.Resolve(src.CoverImageUrl, cdn)))
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => src.BlogPostCategories
                .Where(x => !x.IsDeleted && !x.BlogCategory.IsDeleted)
                .Select(x => x.BlogCategory)));

        CreateMap<BlogPost, BlogPostDetailDto>()
            .IncludeBase<BlogPost, BlogPostSummaryDto>();
    }
}
