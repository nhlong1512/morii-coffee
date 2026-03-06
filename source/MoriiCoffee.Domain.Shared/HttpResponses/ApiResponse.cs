using Newtonsoft.Json;

namespace MoriiCoffee.Domain.Shared.HttpResponses;

/// <summary>
/// Base class for all standardized API responses.
/// </summary>
public class ApiResponse
{
    public ApiResponse(int statusCode, string? message = null, object? data = null)
    {
        StatusCode = statusCode;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        Data = data;
    }

    public int StatusCode { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Message { get; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object? Data { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IEnumerable<string>? Errors { get; set; }

    private static string GetDefaultMessageForStatusCode(int statusCode) =>
        statusCode switch
        {
            200 => "Success",
            201 => "Created",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Resource Not Found",
            500 => "An unhandled error occurred",
            _ => "Unknown"
        };
}
