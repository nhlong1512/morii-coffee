namespace MoriiCoffee.Application.SeedWork.Exceptions;

/// <summary>
/// Thrown when a request is not authenticated.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
        StatusCode = 401;
    }

    public int StatusCode { get; }
}
