namespace MoriiCoffee.Application.SeedWork.Exceptions;

/// <summary>
/// Thrown when a required external service (e.g., Redis) is unavailable.
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message) : base(message)
    {
        StatusCode = 503;
    }

    public ServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = 503;
    }

    public int StatusCode { get; }
}
