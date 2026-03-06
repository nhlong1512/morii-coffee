using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Newtonsoft.Json;

namespace MoriiCoffee.Presentation.Middlewares;

/// <summary>
/// Global exception-handling middleware.
/// Converts known application exceptions into consistent JSON error responses
/// and catches unexpected exceptions to prevent leaking stack traces.
/// </summary>
public class ErrorWrappingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorWrappingMiddleware> _logger;

    public ErrorWrappingMiddleware(RequestDelegate next, ILogger<ErrorWrappingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            await WriteJsonResponse(context, ex.StatusCode, new ApiNotFoundResponse(ex.Message));
        }
        catch (BadRequestException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            await WriteJsonResponse(context, ex.StatusCode, new ApiBadRequestResponse(ex.Message));
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "{Message}", ex.Message);
            await WriteJsonResponse(context, ex.StatusCode, new ApiUnauthorizedResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode = 500;
        }

        if (!context.Response.HasStarted)
        {
            context.Response.ContentType = "application/json";
            var fallback = new ApiResponse(context.Response.StatusCode);
            await context.Response.WriteAsync(JsonConvert.SerializeObject(fallback));
        }
    }

    private static async Task WriteJsonResponse(HttpContext context, int statusCode, object body)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonConvert.SerializeObject(body));
    }
}
