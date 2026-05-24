using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Shipping.HandleShippingWebhookEvent;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

[ApiController]
[Route("api/v1/shipping/ghn/webhook")]
[AllowAnonymous]
[Produces("application/json")]
public class ShippingWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShippingWebhookController> _logger;

    public ShippingWebhookController(IMediator mediator, ILogger<ShippingWebhookController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [SwaggerOperation(Summary = "[GHN] Receive an order-status webhook event")]
    [SwaggerResponse(200, "Webhook processed or safely ignored.")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, leaveOpen: false))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        var result = await _mediator.Send(
            new HandleShippingWebhookEventCommand
            {
                RawBody = rawBody
            },
            cancellationToken);

        _logger.LogInformation(
            "GHN webhook processed with result {Result}. ProviderOrderCode={ProviderOrderCode} ClientOrderCode={ClientOrderCode}",
            result.Result,
            result.ProviderOrderCode,
            result.ClientOrderCode);

        return Ok(new
        {
            received = true,
            result = result.Result
        });
    }
}
