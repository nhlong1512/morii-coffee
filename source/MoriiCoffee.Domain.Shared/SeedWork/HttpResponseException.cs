namespace MoriiCoffee.Domain.Shared.SeedWork;

/// <summary>
/// Represents an HTTP-level exception that carries a status code and message.
/// </summary>
public class HttpResponseException : Exception
{
    public HttpResponseException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
