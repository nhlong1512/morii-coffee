using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Store.GetAdminStoreById;

/// <summary>Handles <see cref="GetAdminStoreByIdQuery"/> returning a single store for admin.</summary>
public class GetAdminStoreByIdQueryHandler : IQueryHandler<GetAdminStoreByIdQuery, StoreDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAdminStoreByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<StoreDto> Handle(GetAdminStoreByIdQuery request, CancellationToken cancellationToken)
    {
        var store = await _unitOfWork.Stores
            .FindByCondition(s => s.Id == request.Id, trackChanges: false)
            .Include(s => s.OpeningHours)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Store", request.Id);

        return _mapper.Map<StoreDto>(store);
    }
}
