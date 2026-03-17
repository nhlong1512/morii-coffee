using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Payment.GetUserPaymentHistory;

/// <summary>Returns a paginated payment history for the authenticated user, newest first.</summary>
public record GetUserPaymentHistoryQuery(Guid UserId, PaginationFilter Filter) : IQuery<Pagination<PaymentDto>>;
