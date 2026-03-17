using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Payment.GetPaymentStatus;

/// <summary>Returns the current status and details of a payment. Verifies ownership.</summary>
public record GetPaymentStatusQuery(Guid PaymentId, Guid UserId) : IQuery<PaymentDto>;
