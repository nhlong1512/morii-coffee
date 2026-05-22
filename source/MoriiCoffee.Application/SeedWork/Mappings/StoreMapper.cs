using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.Aggregates.StoreAggregate;
using MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;

namespace MoriiCoffee.Application.SeedWork.Mappings;

/// <summary>
/// AutoMapper profile for the Store aggregate.
/// Maps Store → StoreDto and StoreOpeningHours → StoreOpeningHoursDto.
/// Note: DistanceKm is computed in query handlers and must be ignored here.
/// </summary>
public class StoreMapper : Profile
{
    public StoreMapper()
    {
        CreateMap<Store, StoreDto>()
            .ForMember(dest => dest.DistanceKm, opt => opt.Ignore())
            .ForMember(
                dest => dest.OpeningHours,
                opt => opt.MapFrom(src => src.OpeningHours.OrderBy(hours => hours.DayOfWeek)));

        CreateMap<StoreOpeningHours, StoreOpeningHoursDto>();
    }
}
