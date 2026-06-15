namespace MoriiCoffee.Domain.Shared.Enums.Order;

public enum EPaymentProviderEventKind
{
    Unknown = 0,
    PaymentSucceeded = 1,
    PaymentFailed = 2,
    PaymentExpired = 3,
    RefundUpdated = 4
}
