using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Store.GetPublicStoreById;

/// <summary>Handles <see cref="GetPublicStoreByIdQuery"/>. Returns 404 if store is inactive or not found.</summary>
public class GetPublicStoreByIdQueryHandler : IQueryHandler<GetPublicStoreByIdQuery, StoreDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublicStoreByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<StoreDto> Handle(GetPublicStoreByIdQuery request, CancellationToken cancellationToken)
    {
        var store = await _unitOfWork.Stores
            .FindByCondition(s => s.Id == request.Id && s.IsActive, trackChanges: false)
            .Include(s => s.OpeningHours)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Store", request.Id);

        return _mapper.Map<StoreDto>(store);
    }
}
