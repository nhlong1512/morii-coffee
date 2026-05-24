namespace MoriiCoffee.Application.Services.Shipping;

public class ShipmentClientOrderCodeGenerator
{
    public string Generate(Guid orderId, string orderNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        return $"MORII-{orderNumber.Trim()}-{orderId.ToString("N")[..8]}";
    }
}
