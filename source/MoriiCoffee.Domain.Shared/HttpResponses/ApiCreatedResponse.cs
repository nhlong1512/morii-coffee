namespace MoriiCoffee.Domain.Shared.HttpResponses;

/// <summary>Represents a 201 Created API response.</summary>
public class ApiCreatedResponse : ApiResponse
{
    public ApiCreatedResponse(object? data = null, string? message = null)
        : base(201, message, data)
    {
    }
}
