namespace MoriiCoffee.Application.SeedWork.Exceptions;

/// <summary>
/// Thrown when a request contains invalid data or violates a business rule.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
        StatusCode = 400;
    }

    public int StatusCode { get; }
}
