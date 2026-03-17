using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Payment.GetPaymentStatus;

/// <summary>Fetches a payment record and verifies it belongs to the requesting user.</summary>
public class GetPaymentStatusQueryHandler : IQueryHandler<GetPaymentStatusQuery, PaymentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPaymentStatusQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PaymentDto> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId)
            ?? throw new NotFoundException("Payment", request.PaymentId);

        if (payment.UserId != request.UserId)
            throw new UnauthorizedException("You do not have permission to view this payment.");

        return _mapper.Map<PaymentDto>(payment);
    }
}
