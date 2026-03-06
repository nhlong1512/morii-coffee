namespace MoriiCoffee.Domain.Shared.HttpResponses;

/// <summary>Represents a 401 Unauthorized API response.</summary>
public class ApiUnauthorizedResponse : ApiResponse
{
    public ApiUnauthorizedResponse(string message)
        : base(401, message)
    {
    }
}
