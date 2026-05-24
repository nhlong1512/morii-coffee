using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;

namespace MoriiCoffee.Infrastructure.Services.Shipping;

public class GhnShippingGateway : IShippingGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<GhnShippingGateway> _logger;

    public GhnShippingGateway(HttpClient httpClient, ILogger<GhnShippingGateway> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ShippingGatewayProvince>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/shiip/public-api/master-data/province", cancellationToken);
        var payload = await ReadAsync<GhnEnvelope<List<GhnProvinceResponse>>>(response, cancellationToken);
        return payload.Data?
            .Select(item => new ShippingGatewayProvince
            {
                ProvinceId = item.ProvinceId,
                ProvinceName = item.ProvinceName,
                Code = item.Code,
                IsActive = item.Status == 1
            })
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<ShippingGatewayDistrict>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        var request = new { province_id = provinceId };
        var response = await _httpClient.GetAsync(
            $"/shiip/public-api/master-data/district?province_id={provinceId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            response = await _httpClient.PostAsJsonAsync("/shiip/public-api/master-data/district", request, JsonOptions, cancellationToken);
        }

        var payload = await ReadAsync<GhnEnvelope<List<GhnDistrictResponse>>>(response, cancellationToken);
        return payload.Data?
            .Select(item => new ShippingGatewayDistrict
            {
                DistrictId = item.DistrictId,
                ProvinceId = item.ProvinceId,
                DistrictName = item.DistrictName,
                SupportType = item.SupportType,
                IsActive = item.Status == 1
            })
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<ShippingGatewayWard>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/shiip/public-api/master-data/ward",
            new { district_id = districtId },
            JsonOptions,
            cancellationToken);

        var payload = await ReadAsync<GhnEnvelope<List<GhnWardResponse>>>(response, cancellationToken);
        return payload.Data?
            .Select(item => new ShippingGatewayWard
            {
                WardCode = item.WardCode,
                DistrictId = item.DistrictId,
                WardName = item.WardName,
                IsActive = item.Status == 1
            })
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<ShippingGatewayService>> GetAvailableServicesAsync(
        ShippingGatewayAvailableServicesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/shiip/public-api/v2/shipping-order/available-services",
            new
            {
                shop_id = request.ShopId,
                from_district = request.FromDistrictId,
                to_district = request.ToDistrictId
            },
            JsonOptions,
            cancellationToken);

        var payload = await ReadAsync<GhnEnvelope<List<GhnServiceResponse>>>(response, cancellationToken);
        return payload.Data?
            .Select(item => new ShippingGatewayService
            {
                ServiceId = item.ServiceId,
                ServiceTypeId = item.ServiceTypeId,
                ShortName = item.ShortName ?? $"Service {item.ServiceId}",
                DisplayName = string.IsNullOrWhiteSpace(item.ShortName)
                    ? $"GHN Service {item.ServiceId}"
                    : $"GHN {item.ShortName}"
            })
            .ToList() ?? [];
    }

    public async Task<ShippingGatewayFeeQuote> CalculateFeeAsync(
        ShippingGatewayFeeRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/shiip/public-api/v2/shipping-order/fee")
        {
            Content = JsonContent.Create(new
            {
                from_district_id = request.FromDistrictId,
                from_ward_code = request.FromWardCode,
                service_id = request.ServiceId,
                service_type_id = request.ServiceTypeId,
                to_district_id = request.ToDistrictId,
                to_ward_code = request.ToWardCode,
                height = request.HeightCm,
                length = request.LengthCm,
                weight = request.WeightGrams,
                width = request.WidthCm,
                insurance_value = ToProviderIntAmount(request.InsuranceValue),
                cod_failed_amount = 0,
                items = request.Items.Select(item => new
                {
                    name = item.Name,
                    quantity = item.Quantity,
                    height = item.HeightCm,
                    weight = item.WeightGrams,
                    length = item.LengthCm,
                    width = item.WidthCm
                }).ToList()
            }, options: JsonOptions)
        };
        message.Headers.Add("ShopId", request.ShopId.ToString());

        var response = await _httpClient.SendAsync(message, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = Deserialize<GhnEnvelope<GhnFeeResponse>>(raw);
        EnsureSuccess(response, payload, "calculate fee");

        var fee = payload.Data ?? throw new BadRequestException("GHN did not return a fee quote.");
        return new ShippingGatewayFeeQuote
        {
            TotalFee = fee.Total,
            ServiceFee = fee.ServiceFee,
            InsuranceFee = fee.InsuranceFee,
            StationFee = fee.StationFee,
            PickStationFee = fee.PickStationFee,
            CouponValue = fee.CouponValue,
            R2SFee = fee.R2SFee,
            ReturnAgain = fee.ReturnAgain,
            DocumentReturn = fee.DocumentReturn,
            DoubleCheck = fee.DoubleCheck,
            CodFee = fee.CodFee,
            RawPayload = raw
        };
    }

    public async Task<ShippingGatewayLeadTimeQuote?> CalculateLeadTimeAsync(
        ShippingGatewayLeadTimeRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/shiip/public-api/v2/shipping-order/leadtime",
            new
            {
                from_district_id = request.FromDistrictId,
                from_ward_code = request.FromWardCode,
                to_district_id = request.ToDistrictId,
                to_ward_code = request.ToWardCode,
                service_id = request.ServiceId
            },
            JsonOptions,
            cancellationToken);

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("GHN lead time request failed with status {StatusCode}: {Body}", response.StatusCode, raw);
            return null;
        }

        var payload = Deserialize<GhnEnvelope<GhnLeadTimeResponse>>(raw);
        if (payload.Data is null)
            return null;

        return new ShippingGatewayLeadTimeQuote
        {
            EstimatedDeliveryAtUtc = DateTimeOffset.FromUnixTimeSeconds(payload.Data.LeadTime).UtcDateTime,
            RawPayload = raw
        };
    }

    public async Task<ShippingGatewayCreateShipmentResult> CreateShipmentAsync(
        ShippingGatewayCreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/shiip/public-api/v2/shipping-order/create")
        {
            Content = JsonContent.Create(new
            {
                payment_type_id = 2,
                note = request.Note,
                required_note = "KHONGCHOXEMHANG",
                client_order_code = request.ClientOrderCode,
                to_name = request.ToName,
                to_phone = request.ToPhone,
                to_address = request.ToAddress,
                to_ward_code = request.ToWardCode,
                to_district_id = request.ToDistrictId,
                cod_amount = ToProviderIntAmount(request.CodAmount),
                content = request.ClientOrderCode,
                weight = request.WeightGrams,
                length = request.LengthCm,
                width = request.WidthCm,
                height = request.HeightCm,
                insurance_value = ToProviderIntAmount(request.InsuranceValue),
                service_id = request.ServiceId,
                service_type_id = request.ServiceTypeId,
                items = request.Items.Select(item => new
                {
                    name = item.Name,
                    code = item.Code,
                    quantity = item.Quantity,
                    price = item.Price.HasValue ? (int?)ToProviderIntAmount(item.Price.Value) : null,
                    length = item.LengthCm,
                    width = item.WidthCm,
                    height = item.HeightCm,
                    weight = item.WeightGrams
                }).ToList()
            }, options: JsonOptions)
        };
        message.Headers.Add("ShopId", request.ShopId.ToString());

        var response = await _httpClient.SendAsync(message, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = Deserialize<GhnEnvelope<GhnCreateOrderResponse>>(raw);
        EnsureSuccess(response, payload, "create shipment");

        var data = payload.Data ?? throw new BadRequestException("GHN did not return a shipment payload.");
        return new ShippingGatewayCreateShipmentResult
        {
            ProviderOrderCode = data.OrderCode,
            ClientOrderCode = request.ClientOrderCode,
            Status = "created",
            StatusLabel = "created",
            TotalFee = data.TotalFee,
            ExpectedDeliveryAtUtc = ParseDateTime(data.ExpectedDeliveryTime),
            RawPayload = raw
        };
    }

    public async Task<ShippingGatewayShipmentDetail> GetShipmentDetailAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOrderCode);

        var response = await _httpClient.PostAsJsonAsync(
            "/shiip/public-api/v2/shipping-order/detail",
            new { order_code = providerOrderCode.Trim() },
            JsonOptions,
            cancellationToken);

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = Deserialize<GhnEnvelope<JsonElement>>(raw);
        EnsureSuccess(response, payload, "get shipment detail");
        return MapShipmentDetail(payload.Data, raw);
    }

    public async Task<ShippingGatewayShipmentDetail> GetShipmentDetailByClientOrderCodeAsync(
        string clientOrderCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientOrderCode);

        var response = await _httpClient.PostAsJsonAsync(
            "/shiip/public-api/v2/shipping-order/detail-by-client-code",
            new { client_order_code = clientOrderCode.Trim() },
            JsonOptions,
            cancellationToken);

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = Deserialize<GhnEnvelope<JsonElement>>(raw);
        EnsureSuccess(response, payload, "get shipment detail by client order code");
        return MapShipmentDetail(payload.Data, raw);
    }

    public async Task<ShippingGatewayCancelShipmentResult> CancelShipmentAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOrderCode);

        using var message = new HttpRequestMessage(HttpMethod.Post, "/shiip/public-api/v2/switch-status/cancel")
        {
            Content = JsonContent.Create(new
            {
                order_codes = new[] { providerOrderCode.Trim() }
            }, options: JsonOptions)
        };
        if (_httpClient.DefaultRequestHeaders.TryGetValues("ShopId", out var shopIds))
            message.Headers.Add("ShopId", shopIds.First());

        var response = await _httpClient.SendAsync(message, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = Deserialize<GhnEnvelope<List<GhnCancelOrderResponse>>>(raw);
        EnsureSuccess(response, payload, "cancel shipment");

        var result = payload.Data?.FirstOrDefault()
            ?? throw new BadRequestException("GHN did not return a cancellation result.");

        return new ShippingGatewayCancelShipmentResult
        {
            ProviderOrderCode = result.OrderCode,
            Success = result.Result,
            Message = string.IsNullOrWhiteSpace(result.Message) ? payload.Message ?? "Unknown GHN response." : result.Message,
            RawPayload = raw
        };
    }

    public async Task UpdateShipmentNoteAsync(
        string providerOrderCode,
        string note,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerOrderCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(note);

        using var message = new HttpRequestMessage(HttpMethod.Post, "/shiip/public-api/v2/shipping-order/update")
        {
            Content = JsonContent.Create(new
            {
                order_code = providerOrderCode.Trim(),
                note = note.Trim()
            }, options: JsonOptions)
        };
        if (_httpClient.DefaultRequestHeaders.TryGetValues("ShopId", out var shopIds))
            message.Headers.Add("ShopId", shopIds.First());

        var response = await _httpClient.SendAsync(message, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = Deserialize<GhnEnvelope<JsonElement>>(raw);
        EnsureSuccess(response, payload, "update shipment note");
    }

    private async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = Deserialize<T>(raw);
        if (payload is GhnEnvelope<object> envelope)
            EnsureSuccess(response, envelope, "call GHN");
        return payload;
    }

    private static T Deserialize<T>(string raw)
    {
        var payload = JsonSerializer.Deserialize<T>(raw, JsonOptions);
        return payload ?? throw new BadRequestException("Unable to parse GHN response payload.");
    }

    private static void EnsureSuccess(HttpResponseMessage response, IGhnEnvelope envelope, string operation)
    {
        if (response.IsSuccessStatusCode && envelope.Code == 200)
            return;

        var message = string.IsNullOrWhiteSpace(envelope.Message)
            ? $"GHN failed to {operation}."
            : envelope.Message;
        throw new BadRequestException(message);
    }

    private static ShippingGatewayShipmentDetail MapShipmentDetail(JsonElement payload, string rawPayload)
    {
        var element = payload.ValueKind == JsonValueKind.Array
            ? payload.EnumerateArray().FirstOrDefault()
            : payload;

        var providerOrderCode = TryGetString(element, "order_code")
            ?? throw new BadRequestException("GHN shipment detail payload is missing order_code.");

        return new ShippingGatewayShipmentDetail
        {
            ProviderOrderCode = providerOrderCode,
            ClientOrderCode = TryGetString(element, "client_order_code"),
            Status = TryGetString(element, "status") ?? "unknown",
            StatusLabel = TryGetString(element, "status") ?? "unknown",
            TotalFee = TryGetDecimal(element, "total_fee"),
            ExpectedDeliveryAtUtc = ParseDateTime(TryGetString(element, "leadtime_order") ?? TryGetString(element, "expected_delivery_time")),
            ReasonCode = TryGetString(element, "reason_code"),
            Reason = TryGetString(element, "reason"),
            CodAmount = TryGetDecimal(element, "cod_amount"),
            RawPayload = rawPayload
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => property.ToString()
        };
    }

    private static decimal? TryGetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
            return decimalValue;

        if (property.ValueKind == JsonValueKind.String &&
            decimal.TryParse(property.GetString(), out decimalValue))
        {
            return decimalValue;
        }

        return null;
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTime.TryParse(value, out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static int ToProviderIntAmount(decimal amount)
    {
        return decimal.ToInt32(decimal.Round(amount, 0, MidpointRounding.AwayFromZero));
    }

    private interface IGhnEnvelope
    {
        int Code { get; }

        string? Message { get; }
    }

    private sealed class GhnEnvelope<T> : IGhnEnvelope
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private sealed class GhnProvinceResponse
    {
        [JsonPropertyName("ProvinceID")]
        public int ProvinceId { get; set; }

        [JsonPropertyName("ProvinceName")]
        public string ProvinceName { get; set; } = null!;

        [JsonPropertyName("Code")]
        public string? Code { get; set; }

        [JsonPropertyName("Status")]
        public int Status { get; set; }
    }

    private sealed class GhnDistrictResponse
    {
        [JsonPropertyName("DistrictID")]
        public int DistrictId { get; set; }

        [JsonPropertyName("ProvinceID")]
        public int ProvinceId { get; set; }

        [JsonPropertyName("DistrictName")]
        public string DistrictName { get; set; } = null!;

        [JsonPropertyName("SupportType")]
        public int? SupportType { get; set; }

        [JsonPropertyName("Status")]
        public int Status { get; set; }
    }

    private sealed class GhnWardResponse
    {
        [JsonPropertyName("WardCode")]
        public string WardCode { get; set; } = null!;

        [JsonPropertyName("DistrictID")]
        public int DistrictId { get; set; }

        [JsonPropertyName("WardName")]
        public string WardName { get; set; } = null!;

        [JsonPropertyName("Status")]
        public int Status { get; set; }
    }

    private sealed class GhnServiceResponse
    {
        [JsonPropertyName("service_id")]
        public int ServiceId { get; set; }

        [JsonPropertyName("service_type_id")]
        public int? ServiceTypeId { get; set; }

        [JsonPropertyName("short_name")]
        public string? ShortName { get; set; }
    }

    private sealed class GhnFeeResponse
    {
        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("service_fee")]
        public decimal ServiceFee { get; set; }

        [JsonPropertyName("insurance_fee")]
        public decimal InsuranceFee { get; set; }

        [JsonPropertyName("station_fee")]
        public decimal StationFee { get; set; }

        [JsonPropertyName("pick_station_fee")]
        public decimal PickStationFee { get; set; }

        [JsonPropertyName("coupon_value")]
        public decimal CouponValue { get; set; }

        [JsonPropertyName("r2s_fee")]
        public decimal R2SFee { get; set; }

        [JsonPropertyName("return_again")]
        public decimal ReturnAgain { get; set; }

        [JsonPropertyName("document_return")]
        public decimal DocumentReturn { get; set; }

        [JsonPropertyName("double_check")]
        public decimal DoubleCheck { get; set; }

        [JsonPropertyName("cod_fee")]
        public decimal CodFee { get; set; }
    }

    private sealed class GhnLeadTimeResponse
    {
        [JsonPropertyName("leadtime")]
        public long LeadTime { get; set; }
    }

    private sealed class GhnCreateOrderResponse
    {
        [JsonPropertyName("expected_delivery_time")]
        public string? ExpectedDeliveryTime { get; set; }

        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; } = null!;

        [JsonPropertyName("total_fee")]
        public decimal? TotalFee { get; set; }
    }

    private sealed class GhnCancelOrderResponse
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; } = null!;

        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = null!;
    }
}
