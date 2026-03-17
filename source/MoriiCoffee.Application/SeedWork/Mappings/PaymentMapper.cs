using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;

namespace MoriiCoffee.Application.SeedWork.Mappings;

/// <summary>AutoMapper profile for the Payment aggregate.</summary>
public class PaymentMapper : Profile
{
    public PaymentMapper()
    {
        CreateMap<Payment, PaymentDto>();
    }
}
