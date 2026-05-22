namespace MoriiCoffee.Application.SeedWork.Exceptions;

/// <summary>
/// Thrown when a request conflicts with an existing resource state.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
        StatusCode = 409;
    }

    public int StatusCode { get; }
}
