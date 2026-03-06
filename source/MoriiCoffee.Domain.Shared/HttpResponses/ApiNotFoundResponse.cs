namespace MoriiCoffee.Domain.Shared.HttpResponses;

/// <summary>Represents a 404 Not Found API response.</summary>
public class ApiNotFoundResponse : ApiResponse
{
    public ApiNotFoundResponse(string message)
        : base(404, message)
    {
    }
}
