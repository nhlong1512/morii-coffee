using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.Commands.Shipping.CreateShippingQuote;

public class CreateShippingQuoteCommandHandler : ICommandHandler<CreateShippingQuoteCommand, ShippingQuoteDto?>
{
    private readonly ICartService _cartService;
    private readonly IShippingGateway _shippingGateway;
    private readonly GhnSettings _ghnSettings;
    private readonly ShippingPackageMetricsService _packageMetricsService;
    private readonly ShippingQuoteFingerprintService _fingerprintService;

    public CreateShippingQuoteCommandHandler(
        ICartService cartService,
        IShippingGateway shippingGateway,
        GhnSettings ghnSettings,
        ShippingPackageMetricsService packageMetricsService,
        ShippingQuoteFingerprintService fingerprintService)
    {
        _cartService = cartService;
        _shippingGateway = shippingGateway;
        _ghnSettings = ghnSettings;
        _packageMetricsService = packageMetricsService;
        _fingerprintService = fingerprintService;
    }

    public async Task<ShippingQuoteDto?> Handle(CreateShippingQuoteCommand request, CancellationToken cancellationToken)
    {
        if (request.DeliveryMethod == EDeliveryMethod.PICKUP)
            return null;

        var cart = await _cartService.GetCartAsync(request.UserId);
        if (cart.Items.Count == 0)
            throw new BadRequestException("Cart is empty.");

        if (request.Address.DistrictId is null || string.IsNullOrWhiteSpace(request.Address.WardCode))
            throw new BadRequestException("District and ward are required for GHN delivery.");

        var packageMetrics = _packageMetricsService.BuildFromCart(cart.Items);
        var availableServices = await _shippingGateway.GetAvailableServicesAsync(
            new ShippingGatewayAvailableServicesRequest
            {
                ShopId = _ghnSettings.ShopId,
                FromDistrictId = _ghnSettings.FromDistrictId,
                ToDistrictId = request.Address.DistrictId.Value
            },
            cancellationToken);

        if (availableServices.Count == 0)
            throw new BadRequestException("GHN does not support delivery for the selected route.");

        var selectedService = SelectService(availableServices, request.SelectedServiceId, _ghnSettings.DefaultServiceTypeId);
        var leadTime = await _shippingGateway.CalculateLeadTimeAsync(
            new ShippingGatewayLeadTimeRequest
            {
                FromDistrictId = _ghnSettings.FromDistrictId,
                FromWardCode = _ghnSettings.FromWardCode,
                ToDistrictId = request.Address.DistrictId.Value,
                ToWardCode = request.Address.WardCode!,
                ServiceId = selectedService.ServiceId
            },
            cancellationToken);

        var feeQuote = await _shippingGateway.CalculateFeeAsync(
            new ShippingGatewayFeeRequest
            {
                ShopId = _ghnSettings.ShopId,
                FromDistrictId = _ghnSettings.FromDistrictId,
                FromWardCode = _ghnSettings.FromWardCode,
                ToDistrictId = request.Address.DistrictId.Value,
                ToWardCode = request.Address.WardCode!,
                ServiceId = selectedService.ServiceId,
                ServiceTypeId = selectedService.ServiceTypeId,
                LengthCm = packageMetrics.LengthCm,
                WidthCm = packageMetrics.WidthCm,
                HeightCm = packageMetrics.HeightCm,
                WeightGrams = packageMetrics.TotalWeightGrams,
                InsuranceValue = packageMetrics.InsuranceValue,
                CodAmount = request.PaymentMethod == MoriiCoffee.Domain.Shared.Enums.Order.EPaymentMethod.COD
                    ? cart.Items.Sum(x => x.UnitPrice * x.Quantity)
                    : 0,
                Items = cart.Items.Select(item => new ShippingGatewayPackageItem
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
        var quoteFingerprint = _fingerprintService.Generate(
            request.DeliveryMethod,
            request.PaymentMethod,
            request.Address,
            packageMetrics,
            selectedService.ServiceId,
            selectedService.ServiceTypeId,
            quoteExpiresAt);

        return new ShippingQuoteDto
        {
            Provider = EShippingProvider.GHN,
            Environment = string.IsNullOrWhiteSpace(_ghnSettings.Environment) ? "sandbox" : _ghnSettings.Environment,
            Address = request.Address,
            PackageMetrics = packageMetrics,
            Service = new ShippingServiceOptionDto
            {
                ServiceId = selectedService.ServiceId,
                ServiceTypeId = selectedService.ServiceTypeId,
                ShortName = selectedService.ShortName,
                DisplayName = selectedService.DisplayName,
                EstimatedLeadTime = leadTime?.EstimatedDeliveryAtUtc,
                Fee = feeQuote.TotalFee,
                IsRecommended = true
            },
            AvailableServices = availableServices.Select(service => new ShippingServiceOptionDto
            {
                ServiceId = service.ServiceId,
                ServiceTypeId = service.ServiceTypeId,
                ShortName = service.ShortName,
                DisplayName = service.DisplayName,
                EstimatedLeadTime = service.ServiceId == selectedService.ServiceId ? leadTime?.EstimatedDeliveryAtUtc : null,
                Fee = service.ServiceId == selectedService.ServiceId ? feeQuote.TotalFee : null,
                IsRecommended = service.ServiceId == selectedService.ServiceId
            }).ToList(),
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
            QuoteFingerprint = quoteFingerprint
        };
    }

    private static ShippingGatewayService SelectService(
        IReadOnlyList<ShippingGatewayService> services,
        int? selectedServiceId,
        int defaultServiceTypeId)
    {
        if (selectedServiceId.HasValue)
        {
            var exact = services.FirstOrDefault(x => x.ServiceId == selectedServiceId.Value);
            if (exact is not null)
                return exact;
        }

        if (defaultServiceTypeId > 0)
        {
            var preferred = services.FirstOrDefault(x => x.ServiceTypeId == defaultServiceTypeId);
            if (preferred is not null)
                return preferred;
        }

        return services[0];
    }
}
