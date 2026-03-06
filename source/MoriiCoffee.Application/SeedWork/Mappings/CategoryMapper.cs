using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;

namespace MoriiCoffee.Application.SeedWork.Mappings;

public class CategoryMapper : Profile
{
    public CategoryMapper()
    {
        CreateMap<Category, CategoryDto>();
    }
}
