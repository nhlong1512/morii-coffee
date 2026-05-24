using System.Text.Json;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Commands.Shipping.HandleShippingWebhookEvent;

public class HandleShippingWebhookEventCommandHandler
    : ICommandHandler<HandleShippingWebhookEventCommand, HandleShippingWebhookEventResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentStatusMapper _statusMapper;
    private readonly ILogger<HandleShippingWebhookEventCommandHandler> _logger;

    public HandleShippingWebhookEventCommandHandler(
        IUnitOfWork unitOfWork,
        ShipmentStatusMapper statusMapper,
        ILogger<HandleShippingWebhookEventCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _statusMapper = statusMapper;
        _logger = logger;
    }

    public async Task<HandleShippingWebhookEventResult> Handle(
        HandleShippingWebhookEventCommand request,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(request.RawBody);
        var root = document.RootElement;

        var eventType = GetString(root, "Type") ?? "unknown";
        var providerOrderCode = GetString(root, "OrderCode");
        var clientOrderCode = GetString(root, "ClientOrderCode");
        var providerEventId = BuildProviderEventId(
            eventType,
            GetString(root, "Status"),
            GetString(root, "Time"),
            providerOrderCode,
            clientOrderCode);

        if (await _unitOfWork.ShipmentWebhookEvents.ExistsAsync(
            providerEventId,
            providerOrderCode,
            clientOrderCode,
            eventType))
        {
            return new HandleShippingWebhookEventResult
            {
                Result = "duplicate",
                EventType = eventType,
                ProviderOrderCode = providerOrderCode,
                ClientOrderCode = clientOrderCode
            };
        }

        var auditRow = ShipmentWebhookEvent.Create(
            EShippingProvider.GHN,
            eventType,
            request.RawBody,
            providerEventId,
            providerOrderCode,
            clientOrderCode,
            signatureVerified: false);
        await _unitOfWork.ShipmentWebhookEvents.CreateAsync(auditRow);

        var shipment = !string.IsNullOrWhiteSpace(providerOrderCode)
            ? await _unitOfWork.Shipments.GetByProviderOrderCodeAsync(providerOrderCode)
            : null;

        shipment ??= !string.IsNullOrWhiteSpace(clientOrderCode)
            ? await _unitOfWork.Shipments.GetByClientOrderCodeAsync(clientOrderCode)
            : null;

        if (shipment is null)
        {
            auditRow.MarkProcessed("shipment_not_found", DateTime.UtcNow);
            await _unitOfWork.ShipmentWebhookEvents.Update(auditRow);
            await _unitOfWork.CommitAsync();

            _logger.LogWarning(
                "GHN webhook {EventType} could not find shipment. ProviderOrderCode={ProviderOrderCode} ClientOrderCode={ClientOrderCode}",
                eventType,
                providerOrderCode,
                clientOrderCode);

            return new HandleShippingWebhookEventResult
            {
                Result = "shipment_not_found",
                EventType = eventType,
                ProviderOrderCode = providerOrderCode,
                ClientOrderCode = clientOrderCode
            };
        }

        var providerStatus = GetString(root, "Status");
        shipment.ApplyProviderUpdate(
            _statusMapper.Map(providerStatus),
            providerStatus ?? eventType,
            providerOrderCode,
            GetDecimal(root, "TotalFee"),
            GetDateTime(root, "Time"),
            shipment.TrackingUrl,
            request.RawBody,
            GetString(root, "ReasonCode"),
            GetString(root, "Reason"),
            DateTime.UtcNow);

        await _unitOfWork.Shipments.Update(shipment);
        auditRow.MarkProcessed("processed", DateTime.UtcNow);
        await _unitOfWork.ShipmentWebhookEvents.Update(auditRow);
        await _unitOfWork.CommitAsync();

        return new HandleShippingWebhookEventResult
        {
            Result = "processed",
            EventType = eventType,
            ProviderOrderCode = providerOrderCode,
            ClientOrderCode = clientOrderCode
        };
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => property.GetString(),
            _ => property.ToString()
        };
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var value))
            return value;

        return property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), out value)
            ? value
            : null;
    }

    private static DateTime? GetDateTime(JsonElement element, string propertyName)
    {
        var raw = GetString(element, propertyName);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return DateTime.TryParse(raw, out var parsed) ? parsed.ToUniversalTime() : null;
    }

    private static string BuildProviderEventId(
        string eventType,
        string? status,
        string? time,
        string? providerOrderCode,
        string? clientOrderCode)
    {
        return string.Join(
            ":",
            new[]
            {
                eventType,
                status ?? "unknown",
                time ?? "no-time",
                providerOrderCode ?? "no-provider-code",
                clientOrderCode ?? "no-client-code"
            });
    }
}
