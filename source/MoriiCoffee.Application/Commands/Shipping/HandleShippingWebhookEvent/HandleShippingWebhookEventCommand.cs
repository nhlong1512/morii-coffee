using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Shipping.HandleShippingWebhookEvent;

public class HandleShippingWebhookEventCommand : ICommand<HandleShippingWebhookEventResult>
{
    public string RawBody { get; set; } = null!;
}

public class HandleShippingWebhookEventResult
{
    public string Result { get; set; } = null!;

    public string? EventType { get; set; }

    public string? ProviderOrderCode { get; set; }

    public string? ClientOrderCode { get; set; }
}
