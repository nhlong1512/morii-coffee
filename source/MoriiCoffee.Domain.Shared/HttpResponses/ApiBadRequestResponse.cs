namespace MoriiCoffee.Domain.Shared.HttpResponses;

/// <summary>Represents a 400 Bad Request API response.</summary>
public class ApiBadRequestResponse : ApiResponse
{
    public ApiBadRequestResponse(string message)
        : base(400, message)
    {
    }
}
