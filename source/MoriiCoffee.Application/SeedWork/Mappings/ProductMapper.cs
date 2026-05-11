using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.SeedWork.Mappings;

public class ProductMapper : Profile
{
    public ProductMapper(AwsS3Settings s3Settings)
    {
        var cdn = s3Settings.CdnBaseUrl;

        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => CdnUrlHelper.Resolve(src.ThumbnailUrl, cdn)))
            .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants.OrderBy(v => v.AdditionalPrice)))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images.OrderBy(i => i.DisplayOrder)))
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => src.ProductCategories.Select(pc => pc.Category)));

        CreateMap<Product, ProductSummaryDto>()
            .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => CdnUrlHelper.Resolve(src.ThumbnailUrl, cdn)))
            .ForMember(dest => dest.CategoryNames, opt => opt.MapFrom(src => src.ProductCategories.Where(pc => !pc.IsDeleted).Select(pc => pc.Category.Name)));

        CreateMap<ProductVariant, ProductVariantDto>()
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore());

        CreateMap<ProductImage, ProductImageDto>()
            .ForMember(dest => dest.Url, opt => opt.MapFrom(src => CdnUrlHelper.Resolve(src.Url, cdn)));
    }
}
