using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Shipping.GetShippingDistricts;

public record GetShippingDistrictsQuery(int ProvinceId) : IQuery<List<ShippingDistrictDto>>;
