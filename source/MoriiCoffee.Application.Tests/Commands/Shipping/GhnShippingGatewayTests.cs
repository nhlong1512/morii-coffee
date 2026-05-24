using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Infrastructure.Services.Shipping;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Shipping;

public class GhnShippingGatewayTests
{
    [Fact]
    public async Task CalculateFeeAsync_SerializesInsuranceValueAsInteger()
    {
        var handler = new CaptureHandler("""
        {"code":200,"message":"Success","data":{"total":20900,"service_fee":20900,"insurance_fee":0,"pick_station_fee":0,"coupon_value":0,"r2s_fee":0,"return_again":0,"document_return":0,"double_check":0,"cod_fee":0}}
        """);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://dev-online-gateway.ghn.vn")
        };
        var gateway = new GhnShippingGateway(client, NullLogger<GhnShippingGateway>.Instance);

        await gateway.CalculateFeeAsync(new ShippingGatewayFeeRequest
        {
            ShopId = 200400,
            FromDistrictId = 1461,
            FromWardCode = "21310",
            ToDistrictId = 1461,
            ToWardCode = "21310",
            ServiceId = 53320,
            ServiceTypeId = 2,
            LengthCm = 20,
            WidthCm = 20,
            HeightCm = 8,
            WeightGrams = 250,
            InsuranceValue = 137000.00m,
            CodAmount = 0,
            Items =
            [
                new ShippingGatewayPackageItem
                {
                    Name = "A-Me",
                    Quantity = 1,
                    LengthCm = 20,
                    WidthCm = 20,
                    HeightCm = 8,
                    WeightGrams = 250
                }
            ]
        });

        handler.LastBody.Should().Contain("\"insurance_value\":137000");
        handler.LastBody.Should().NotContain("\"insurance_value\":137000.00");
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public CaptureHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
