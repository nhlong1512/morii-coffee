namespace MoriiCoffee.Application.SeedWork.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.")
    {
        StatusCode = 404;
    }

    public NotFoundException(string message) : base(message)
    {
        StatusCode = 404;
    }

    public int StatusCode { get; }
}
