using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.User.GetMyDeliveryProfile;

/// <summary>Returns the saved delivery profile for the requesting user, or null if none has been saved.</summary>
public class GetMyDeliveryProfileQueryHandler : IQueryHandler<GetMyDeliveryProfileQuery, DeliveryProfileDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMyDeliveryProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<DeliveryProfileDto?> Handle(GetMyDeliveryProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.UserDeliveryProfiles.GetByUserIdAsync(request.UserId);

        if (profile is null)
            return null;

        return new DeliveryProfileDto
        {
            FullName = profile.FullName,
            PhoneNumber = profile.PhoneNumber,
            Address = profile.Address,
            ProvinceId = profile.ProvinceId,
            ProvinceName = profile.ProvinceName,
            DistrictId = profile.DistrictId,
            DistrictName = profile.DistrictName,
            WardCode = profile.WardCode,
            WardName = profile.WardName
        };
    }
}
