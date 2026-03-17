using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Notification;
using MoriiCoffee.Domain.Aggregates.NotificationAggregate;

namespace MoriiCoffee.Application.SeedWork.Mappings;

/// <summary>AutoMapper profile for the Notification aggregate.</summary>
public class NotificationMapper : Profile
{
    public NotificationMapper()
    {
        CreateMap<Notification, NotificationDto>();
    }
}
