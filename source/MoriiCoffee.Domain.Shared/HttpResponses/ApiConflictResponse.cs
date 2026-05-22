namespace MoriiCoffee.Domain.Shared.HttpResponses;

/// <summary>Represents a 409 Conflict API response.</summary>
public class ApiConflictResponse : ApiResponse
{
    public ApiConflictResponse(string message)
        : base(409, message)
    {
    }
}
