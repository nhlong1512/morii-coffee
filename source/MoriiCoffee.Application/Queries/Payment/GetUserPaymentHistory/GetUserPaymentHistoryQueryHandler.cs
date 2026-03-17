using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Payment.GetUserPaymentHistory;

/// <summary>Returns a paginated payment history for the caller, ordered by most recent first.</summary>
public class GetUserPaymentHistoryQueryHandler
    : IQueryHandler<GetUserPaymentHistoryQuery, Pagination<PaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserPaymentHistoryQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Pagination<PaymentDto>> Handle(
        GetUserPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var dtoQuery = _unitOfWork.Payments
            .FindByCondition(p => p.UserId == request.UserId)
            .OrderByDescending(p => p.CreatedAt)
            .AsEnumerable()
            .Select(p => _mapper.Map<PaymentDto>(p))
            .AsQueryable();

        var result = PagingHelper.QueryPaginate(request.Filter, dtoQuery);
        return Task.FromResult(result);
    }
}
