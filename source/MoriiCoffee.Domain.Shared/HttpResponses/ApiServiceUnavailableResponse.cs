namespace MoriiCoffee.Domain.Shared.HttpResponses;

/// <summary>Represents a 503 Service Unavailable API response.</summary>
public class ApiServiceUnavailableResponse : ApiResponse
{
    public ApiServiceUnavailableResponse(string message)
        : base(503, message)
    {
    }
}
