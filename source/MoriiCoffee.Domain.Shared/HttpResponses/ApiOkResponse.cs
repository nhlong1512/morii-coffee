namespace MoriiCoffee.Domain.Shared.HttpResponses;

/// <summary>Represents a 200 OK API response.</summary>
public class ApiOkResponse : ApiResponse
{
    public ApiOkResponse(object? data = null, string? message = null)
        : base(200, message, data)
    {
    }
}
