using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.StoreAggregate;
using MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using StoreEntity = MoriiCoffee.Domain.Aggregates.StoreAggregate.Store;

namespace MoriiCoffee.Application.Commands.Store.CreateStore;

/// <summary>Handles <see cref="CreateStoreCommand"/> by creating a new store with 7 opening hours records.</summary>
public class CreateStoreCommandHandler : ICommandHandler<CreateStoreCommand, StoreDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateStoreCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<StoreDto> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
    {
        var slug = GenerateSlug(request.Slug, request.Name);

        if (await _unitOfWork.Stores.SlugExistsAsync(slug, ct: cancellationToken))
            throw new ConflictException($"A store with slug '{slug}' already exists.");

        if (await _unitOfWork.Stores.NameExistsAsync(request.Name, ct: cancellationToken))
            throw new ConflictException($"A store named '{request.Name}' already exists.");

        var data = new CreateStoreData(
            request.Name, request.Slug, request.Address, request.District,
            request.City, request.Province, request.Latitude, request.Longitude,
            request.Phone, request.Email, request.CoverImageUrl, request.IsActive, request.DisplayOrder);

        var store = StoreEntity.Create(data, slug);

        foreach (var h in request.OpeningHours)
        {
            var hours = StoreOpeningHours.Create(store.Id, h.DayOfWeek, h.OpenTime, h.CloseTime, h.IsClosed);
            store.OpeningHours.Add(hours);
        }

        await _unitOfWork.Stores.CreateAsync(store);
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
