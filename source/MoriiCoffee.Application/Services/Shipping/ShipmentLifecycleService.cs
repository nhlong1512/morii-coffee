using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.Services.Shipping;

public class ShipmentLifecycleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShippingGateway _shippingGateway;
    private readonly GhnSettings _ghnSettings;
    private readonly ShippingPackageMetricsService _packageMetricsService;
    private readonly ShipmentClientOrderCodeGenerator _clientOrderCodeGenerator;
    private readonly ShipmentStatusMapper _statusMapper;
    private readonly ShippingQuoteFingerprintService _quoteFingerprintService;
    private readonly ILogger<ShipmentLifecycleService> _logger;

    public ShipmentLifecycleService(
        IUnitOfWork unitOfWork,
        IShippingGateway shippingGateway,
        GhnSettings ghnSettings,
        ShippingPackageMetricsService packageMetricsService,
        ShipmentClientOrderCodeGenerator clientOrderCodeGenerator,
        ShipmentStatusMapper statusMapper,
        ShippingQuoteFingerprintService quoteFingerprintService,
        ILogger<ShipmentLifecycleService> logger)
    {
        _unitOfWork = unitOfWork;
        _shippingGateway = shippingGateway;
        _ghnSettings = ghnSettings;
        _packageMetricsService = packageMetricsService;
        _clientOrderCodeGenerator = clientOrderCodeGenerator;
        _statusMapper = statusMapper;
        _quoteFingerprintService = quoteFingerprintService;
        _logger = logger;
    }

    public async Task<Shipment?> TryCreateForOrderAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.DeliveryMethod != EDeliveryMethod.GHN_DELIVERY)
            return null;

        if (order.ShippingServiceId is null)
            throw new BadRequestException("GHN delivery orders require a selected shipping service before shipment creation.");

        var existing = await _unitOfWork.Shipments.GetByOrderIdAsync(order.Id);
        if (existing is not null && existing.Status is not EShipmentStatus.FAILED_TO_CREATE)
            return existing;

        var shipment = existing ?? Shipment.CreatePending(
            order.Id,
            _clientOrderCodeGenerator.Generate(order.Id, order.OrderNumber),
            string.IsNullOrWhiteSpace(order.ShippingProviderEnvironment) ? _ghnSettings.Environment : order.ShippingProviderEnvironment!,
            order.PaymentMethod == EPaymentMethod.COD ? order.Total : 0,
            _ghnSettings.ShopId,
            order.ShippingServiceId,
            order.ShippingServiceTypeId);

        if (existing is null)
        {
            await _unitOfWork.Shipments.CreateAsync(shipment);
            await _unitOfWork.CommitAsync();
        }

        try
        {
            var packageMetrics = _packageMetricsService.BuildFromOrder(order.Items);
            var providerResult = await _shippingGateway.CreateShipmentAsync(
                BuildCreateRequest(order, shipment.ClientOrderCode, packageMetrics),
                cancellationToken);

            shipment.MarkCreated(
                providerResult.ProviderOrderCode,
                providerResult.StatusLabel,
                providerResult.TotalFee,
                providerResult.ExpectedDeliveryAtUtc,
                BuildTrackingUrl(providerResult.ProviderOrderCode),
                providerResult.RawPayload,
                DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GHN shipment creation failed for order {OrderId}", order.Id);
            shipment.MarkCreateFailed(null, ex.Message, DateTime.UtcNow);
        }

        await _unitOfWork.Shipments.Update(shipment);
        await _unitOfWork.CommitAsync();
        return shipment;
    }

    public async Task<Shipment> SyncAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        var detail = !string.IsNullOrWhiteSpace(shipment.ProviderOrderCode)
            ? await _shippingGateway.GetShipmentDetailAsync(shipment.ProviderOrderCode, cancellationToken)
            : await _shippingGateway.GetShipmentDetailByClientOrderCodeAsync(shipment.ClientOrderCode, cancellationToken);

        shipment.ApplyProviderUpdate(
            _statusMapper.Map(detail.Status),
            detail.StatusLabel,
            detail.ProviderOrderCode,
            detail.TotalFee,
            detail.ExpectedDeliveryAtUtc,
            BuildTrackingUrl(detail.ProviderOrderCode),
            detail.RawPayload,
            detail.ReasonCode,
            detail.Reason,
            DateTime.UtcNow);

        await _unitOfWork.Shipments.Update(shipment);
        await _unitOfWork.CommitAsync();
        return shipment;
    }

    public async Task<Shipment> CancelAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shipment.ProviderOrderCode))
            throw new BadRequestException("Shipment has not been accepted by GHN yet.");

        var result = await _shippingGateway.CancelShipmentAsync(shipment.ProviderOrderCode, cancellationToken);
        if (!result.Success)
            throw new BadRequestException(result.Message);

        shipment.ApplyProviderUpdate(
            EShipmentStatus.CANCELLED,
            "cancel",
            shipment.ProviderOrderCode,
            shipment.FeeTotal,
            shipment.ExpectedDeliveryAt,
            shipment.TrackingUrl,
            result.RawPayload,
            null,
            result.Message,
            DateTime.UtcNow);

        await _unitOfWork.Shipments.Update(shipment);
        await _unitOfWork.CommitAsync();
        return shipment;
    }

    public async Task<Shipment> UpdateNoteAsync(Shipment shipment, string note, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shipment.ProviderOrderCode))
            throw new BadRequestException("Shipment has not been accepted by GHN yet.");

        await _shippingGateway.UpdateShipmentNoteAsync(shipment.ProviderOrderCode, note, cancellationToken);
        shipment.UpdateNote(note);
        await _unitOfWork.Shipments.Update(shipment);
        await _unitOfWork.CommitAsync();
        return shipment;
    }

    public async Task<ShippingQuoteDto> RequoteAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.DeliveryMethod != EDeliveryMethod.GHN_DELIVERY ||
            order.DeliveryInfo.DistrictId is null ||
            string.IsNullOrWhiteSpace(order.DeliveryInfo.WardCode) ||
            order.ShippingServiceId is null)
        {
            throw new BadRequestException("Only GHN delivery orders with a routing snapshot can be requoted.");
        }

        var packageMetrics = _packageMetricsService.BuildFromOrder(order.Items);
        var leadTime = await _shippingGateway.CalculateLeadTimeAsync(
            new ShippingGatewayLeadTimeRequest
            {
                FromDistrictId = _ghnSettings.FromDistrictId,
                FromWardCode = _ghnSettings.FromWardCode,
                ToDistrictId = order.DeliveryInfo.DistrictId.Value,
                ToWardCode = order.DeliveryInfo.WardCode!,
                ServiceId = order.ShippingServiceId.Value
            },
            cancellationToken);

        var feeQuote = await _shippingGateway.CalculateFeeAsync(
            new ShippingGatewayFeeRequest
            {
                ShopId = _ghnSettings.ShopId,
                FromDistrictId = _ghnSettings.FromDistrictId,
                FromWardCode = _ghnSettings.FromWardCode,
                ToDistrictId = order.DeliveryInfo.DistrictId.Value,
                ToWardCode = order.DeliveryInfo.WardCode!,
                ServiceId = order.ShippingServiceId,
                ServiceTypeId = order.ShippingServiceTypeId,
                LengthCm = packageMetrics.LengthCm,
                WidthCm = packageMetrics.WidthCm,
                HeightCm = packageMetrics.HeightCm,
                WeightGrams = packageMetrics.TotalWeightGrams,
                InsuranceValue = packageMetrics.InsuranceValue,
                CodAmount = order.PaymentMethod == EPaymentMethod.COD ? order.Total : 0,
                Items = order.Items.Select(item => new ShippingGatewayPackageItem
                {
                    Name = item.ProductName,
                    Quantity = item.Quantity,
                    LengthCm = packageMetrics.LengthCm,
                    WidthCm = packageMetrics.WidthCm,
                    HeightCm = packageMetrics.HeightCm,
                    WeightGrams = Math.Max(1, packageMetrics.TotalWeightGrams / packageMetrics.ItemCount)
                }).ToList()
            },
            cancellationToken);

        var quoteExpiresAt = DateTime.UtcNow.AddMinutes(15);
        return new ShippingQuoteDto
        {
            Provider = EShippingProvider.GHN,
            Environment = string.IsNullOrWhiteSpace(order.ShippingProviderEnvironment) ? _ghnSettings.Environment : order.ShippingProviderEnvironment!,
            Address = new ShippingAddressDto
            {
                FullName = order.DeliveryInfo.FullName,
                PhoneNumber = order.DeliveryInfo.PhoneNumber,
                AddressLine = order.DeliveryInfo.Address,
                ProvinceId = order.DeliveryInfo.ProvinceId,
                ProvinceName = order.DeliveryInfo.ProvinceName,
                DistrictId = order.DeliveryInfo.DistrictId,
                DistrictName = order.DeliveryInfo.DistrictName,
                WardCode = order.DeliveryInfo.WardCode,
                WardName = order.DeliveryInfo.WardName
            },
            PackageMetrics = packageMetrics,
            Service = new ShippingServiceOptionDto
            {
                ServiceId = order.ShippingServiceId.Value,
                ServiceTypeId = order.ShippingServiceTypeId,
                ShortName = order.ShippingServiceLabel ?? $"GHN Service {order.ShippingServiceId}",
                DisplayName = order.ShippingServiceLabel ?? $"GHN Service {order.ShippingServiceId}",
                EstimatedLeadTime = leadTime?.EstimatedDeliveryAtUtc,
                Fee = feeQuote.TotalFee,
                IsRecommended = true
            },
            AvailableServices =
            [
                new ShippingServiceOptionDto
                {
                    ServiceId = order.ShippingServiceId.Value,
                    ServiceTypeId = order.ShippingServiceTypeId,
                    ShortName = order.ShippingServiceLabel ?? $"GHN Service {order.ShippingServiceId}",
                    DisplayName = order.ShippingServiceLabel ?? $"GHN Service {order.ShippingServiceId}",
                    EstimatedLeadTime = leadTime?.EstimatedDeliveryAtUtc,
                    Fee = feeQuote.TotalFee,
                    IsRecommended = true
                }
            ],
            FeeBreakdown = new ShippingFeeBreakdownDto
            {
                TotalFee = feeQuote.TotalFee,
                ServiceFee = feeQuote.ServiceFee,
                InsuranceFee = feeQuote.InsuranceFee,
                StationFee = feeQuote.StationFee,
                PickStationFee = feeQuote.PickStationFee,
                CouponValue = feeQuote.CouponValue,
                R2SFee = feeQuote.R2SFee,
                ReturnAgainFee = feeQuote.ReturnAgain,
                DocumentReturnFee = feeQuote.DocumentReturn,
                DoubleCheckFee = feeQuote.DoubleCheck,
                CodFee = feeQuote.CodFee,
                RawPayload = feeQuote.RawPayload
            },
            EstimatedDeliveryAt = leadTime?.EstimatedDeliveryAtUtc,
            QuoteExpiresAt = quoteExpiresAt,
            QuoteFingerprint = _quoteFingerprintService.Generate(
                order.DeliveryMethod,
                order.PaymentMethod,
                new ShippingAddressDto
                {
                    FullName = order.DeliveryInfo.FullName,
                    PhoneNumber = order.DeliveryInfo.PhoneNumber,
                    AddressLine = order.DeliveryInfo.Address,
                    ProvinceId = order.DeliveryInfo.ProvinceId,
                    ProvinceName = order.DeliveryInfo.ProvinceName,
                    DistrictId = order.DeliveryInfo.DistrictId,
                    DistrictName = order.DeliveryInfo.DistrictName,
                    WardCode = order.DeliveryInfo.WardCode,
                    WardName = order.DeliveryInfo.WardName
                },
                packageMetrics,
                order.ShippingServiceId.Value,
                order.ShippingServiceTypeId,
                quoteExpiresAt)
        };
    }

    public static ShipmentSummaryDto ToSummaryDto(Shipment shipment)
    {
        return new ShipmentSummaryDto
        {
            Id = shipment.Id,
            Provider = shipment.Provider,
            ProviderEnvironment = shipment.ProviderEnvironment,
            Status = shipment.Status,
            StatusLabel = shipment.StatusLabel,
            ClientOrderCode = shipment.ClientOrderCode,
            ProviderOrderCode = shipment.ProviderOrderCode,
            ShopId = shipment.ShopId,
            ServiceId = shipment.ServiceId,
            ServiceTypeId = shipment.ServiceTypeId,
            FeeTotal = shipment.FeeTotal,
            ExpectedDeliveryAt = shipment.ExpectedDeliveryAt,
            TrackingUrl = shipment.TrackingUrl,
            FailureReasonCode = shipment.FailureReasonCode,
            FailureReason = shipment.FailureReason,
            Note = shipment.Note,
            LastSyncedAt = shipment.LastSyncedAt
        };
    }

    private ShippingGatewayCreateShipmentRequest BuildCreateRequest(
        Order order,
        string clientOrderCode,
        ShippingPackageMetricsDto packageMetrics)
    {
        return new ShippingGatewayCreateShipmentRequest
        {
            ShopId = _ghnSettings.ShopId,
            ClientOrderCode = clientOrderCode,
            ToName = order.DeliveryInfo.FullName,
            ToPhone = order.DeliveryInfo.PhoneNumber,
            ToAddress = order.DeliveryInfo.Address,
            ToWardCode = order.DeliveryInfo.WardCode!,
            ToDistrictId = order.DeliveryInfo.DistrictId!.Value,
            CodAmount = order.PaymentMethod == EPaymentMethod.COD ? order.Total : 0,
            Note = order.Notes,
            ServiceId = order.ShippingServiceId!.Value,
            ServiceTypeId = order.ShippingServiceTypeId,
            WeightGrams = packageMetrics.TotalWeightGrams,
            LengthCm = packageMetrics.LengthCm,
            WidthCm = packageMetrics.WidthCm,
            HeightCm = packageMetrics.HeightCm,
            InsuranceValue = packageMetrics.InsuranceValue,
            Items = order.Items.Select(item => new ShippingGatewayCreateShipmentItem
            {
                Name = item.ProductName,
                Code = item.ProductId.ToString("N"),
                Quantity = item.Quantity,
                Price = item.UnitPrice,
                WeightGrams = Math.Max(1, packageMetrics.TotalWeightGrams / packageMetrics.ItemCount),
                LengthCm = packageMetrics.LengthCm,
                WidthCm = packageMetrics.WidthCm,
                HeightCm = packageMetrics.HeightCm
            }).ToList()
        };
    }

    private static string? BuildTrackingUrl(string? providerOrderCode)
    {
        if (string.IsNullOrWhiteSpace(providerOrderCode))
            return null;

        return $"https://donhang.ghn.vn/?order_code={providerOrderCode.Trim()}";
    }
}
