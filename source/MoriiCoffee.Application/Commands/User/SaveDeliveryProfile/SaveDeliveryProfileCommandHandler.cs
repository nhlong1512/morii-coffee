using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.User.SaveDeliveryProfile;

/// <summary>
/// Creates or updates the current user's default delivery profile (upsert).
/// If no profile exists for the user a new one is created; otherwise the existing one is updated.
/// </summary>
public class SaveDeliveryProfileCommandHandler : ICommandHandler<SaveDeliveryProfileCommand, DeliveryProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public SaveDeliveryProfileCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<DeliveryProfileDto> Handle(SaveDeliveryProfileCommand command, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.UserDeliveryProfiles.GetByUserIdAsync(command.UserId);

        if (existing is null)
        {
            var newProfile = UserDeliveryProfile.Create(
                command.UserId,
                command.FullName,
                command.PhoneNumber,
                command.Address,
                command.ProvinceId,
                command.ProvinceName,
                command.DistrictId,
                command.DistrictName,
                command.WardCode,
                command.WardName);
            await _unitOfWork.UserDeliveryProfiles.UpsertAsync(newProfile);
        }
        else
        {
            existing.Update(
                command.FullName,
                command.PhoneNumber,
                command.Address,
                command.ProvinceId,
                command.ProvinceName,
                command.DistrictId,
                command.DistrictName,
                command.WardCode,
                command.WardName);
            await _unitOfWork.UserDeliveryProfiles.UpsertAsync(existing);
        }

        await _unitOfWork.CommitAsync();

        return new DeliveryProfileDto
        {
            FullName = command.FullName,
            PhoneNumber = command.PhoneNumber,
            Address = command.Address,
            ProvinceId = command.ProvinceId,
            ProvinceName = command.ProvinceName,
            DistrictId = command.DistrictId,
            DistrictName = command.DistrictName,
            WardCode = command.WardCode,
            WardName = command.WardName
        };
    }
}
