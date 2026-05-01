using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.User.GetMyDeliveryProfile;

/// <summary>Query to retrieve the current user's saved delivery profile. Returns null if none exists.</summary>
public record GetMyDeliveryProfileQuery(Guid UserId) : IQuery<DeliveryProfileDto?>;
