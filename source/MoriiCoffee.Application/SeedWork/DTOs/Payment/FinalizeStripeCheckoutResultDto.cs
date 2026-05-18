using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

public class FinalizeStripeCheckoutResultDto
{
    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public Guid PaymentId { get; set; }

    public EPaymentStatus PaymentStatus { get; set; }

    public string SessionId { get; set; } = null!;
}
