using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.StoreAggregate;
using MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Store.UpdateStore;

/// <summary>
/// Handles <see cref="UpdateStoreCommand"/> by updating store fields and replacing all 7 opening hours records.
/// Opening hours are deleted and re-inserted atomically (replace-all strategy).
/// </summary>
public class UpdateStoreCommandHandler : ICommandHandler<UpdateStoreCommand, StoreDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateStoreCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<StoreDto> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
    {
        var store = await _unitOfWork.Stores
            .FindByCondition(s => s.Id == request.Id, trackChanges: true)
            .Include(s => s.OpeningHours)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Store", request.Id);

        var slug = GenerateSlug(request.Slug, request.Name);

        if (await _unitOfWork.Stores.SlugExistsAsync(slug, excludeId: request.Id, ct: cancellationToken))
            throw new ConflictException($"A store with slug '{slug}' already exists.");

        if (await _unitOfWork.Stores.NameExistsAsync(request.Name, excludeId: request.Id, ct: cancellationToken))
            throw new ConflictException($"A store named '{request.Name}' already exists.");

        var data = new CreateStoreData(
            request.Name, request.Slug, request.Address, request.District,
            request.City, request.Province, request.Latitude, request.Longitude,
            request.Phone, request.Email, request.CoverImageUrl, request.IsActive, request.DisplayOrder);

        store.Update(data, slug);

        store.ReplaceOpeningHours(request.OpeningHours.Select(h =>
            new StoreOpeningHoursData(h.DayOfWeek, h.OpenTime, h.CloseTime, h.IsClosed)));

        await _unitOfWork.Stores.Update(store);
        await _unitOfWork.CommitAsync();

        var dto = _mapper.Map<StoreDto>(store);
        return dto;
    }

    private static string GenerateSlug(string? providedSlug, string name)
    {
        if (!string.IsNullOrWhiteSpace(providedSlug))
            return providedSlug.Trim().ToLowerInvariant();

        return System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", "")
            .Replace(' ', '-')
            .Trim('-');
    }
}
