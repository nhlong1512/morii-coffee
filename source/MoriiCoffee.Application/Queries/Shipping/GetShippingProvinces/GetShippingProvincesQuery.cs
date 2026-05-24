using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Shipping.GetShippingProvinces;

public record GetShippingProvincesQuery() : IQuery<List<ShippingProvinceDto>>;
