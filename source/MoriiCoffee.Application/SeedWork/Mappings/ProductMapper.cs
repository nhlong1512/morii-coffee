using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;

namespace MoriiCoffee.Application.SeedWork.Mappings;

public class ProductMapper : Profile
{
    public ProductMapper()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images.OrderBy(i => i.DisplayOrder)))
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => src.ProductCategories.Select(pc => pc.Category)));

        CreateMap<Product, ProductSummaryDto>()
            .ForMember(dest => dest.CategoryNames, opt => opt.MapFrom(src => src.ProductCategories.Select(pc => pc.Category.Name)));

        CreateMap<ProductVariant, ProductVariantDto>()
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore());

        CreateMap<ProductImage, ProductImageDto>();
    }
}
