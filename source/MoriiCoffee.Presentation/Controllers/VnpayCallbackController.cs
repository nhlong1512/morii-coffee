using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MoriiCoffee.Application.Commands.Payment.HandleWebhookEvent;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Presentation.Controllers;

[ApiController]
[Route("api/v1/payments/vnpay")]
[AllowAnonymous]
public sealed class VnpayCallbackController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPaymentGateway _gateway;
    private readonly VnpaySettings _settings;
    private readonly ILogger<VnpayCallbackController> _logger;

    public VnpayCallbackController(
        IMediator mediator,
        IPaymentGatewayResolver resolver,
        VnpaySettings settings,
        ILogger<VnpayCallbackController> logger)
    {
        _mediator = mediator;
        _gateway = resolver.Resolve(EPaymentProvider.Vnpay);
        _settings = settings;
        _logger = logger;
    }

    [HttpGet("ipn")]
    public async Task<IActionResult> Ipn(CancellationToken cancellationToken)
    {
        WebhookEventEnvelope envelope;
        try
        {
            envelope = _gateway.ConstructWebhookEvent(Request.QueryString.Value ?? string.Empty, null);
        }
        catch (PaymentGatewaySignatureException)
        {
            return Ok(new VnpayIpnResponse("97", "Invalid Checksum"));
        }

        var result = await _mediator.Send(new HandleWebhookEventCommand { Envelope = envelope }, cancellationToken);
        var response = result.Result switch
        {
            EPaymentWebhookProcessingResult.OrderNotFound => new VnpayIpnResponse("01", "Order not Found"),
            EPaymentWebhookProcessingResult.Duplicate => new VnpayIpnResponse("02", "Order already confirmed"),
            EPaymentWebhookProcessingResult.Failed when envelope.EventKind == EPaymentProviderEventKind.PaymentSucceeded =>
                new VnpayIpnResponse("04", "Invalid Amount"),
            EPaymentWebhookProcessingResult.Processed => new VnpayIpnResponse("00", "Confirm Success"),
            _ => new VnpayIpnResponse("99", "Unknown error")
        };

        return Ok(response);
    }

    [HttpGet("return")]
    public IActionResult Return()
    {
        var status = "invalid";
        string? txnRef = null;
        try
        {
            var envelope = _gateway.ConstructWebhookEvent(Request.QueryString.Value ?? string.Empty, null);
            status = envelope.EventKind == EPaymentProviderEventKind.PaymentSucceeded ? "success" : "failed";
            txnRef = envelope.ProviderSessionId;
        }
        catch (PaymentGatewaySignatureException ex)
        {
            _logger.LogWarning(ex, "Rejected invalid VNPAY browser return.");
        }

        if (string.IsNullOrWhiteSpace(_settings.StorefrontReturnUrl))
            return Ok(new { status, transactionReference = txnRef });

        return Redirect(QueryHelpers.AddQueryString(_settings.StorefrontReturnUrl, new Dictionary<string, string?>
        {
            ["status"] = status,
            ["transactionReference"] = txnRef
        }));
    }

    public sealed record VnpayIpnResponse(
        [property: JsonPropertyName("RspCode")] string RspCode,
        [property: JsonPropertyName("Message")] string Message);
}
