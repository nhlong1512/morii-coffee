using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Shipping.GetShippingWards;

public record GetShippingWardsQuery(int DistrictId) : IQuery<List<ShippingWardDto>>;
