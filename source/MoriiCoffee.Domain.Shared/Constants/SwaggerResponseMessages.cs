namespace MoriiCoffee.Domain.Shared.Constants;

/// <summary>
/// Standard Swagger response messages for common HTTP status codes.
/// </summary>
public static class SwaggerResponseMessages
{
    // Success Messages
    public const string CreatedSuccessfully = "Created successfully";
    public const string UpdatedSuccessfully = "Updated successfully";
    public const string DeletedSuccessfully = "Deleted successfully";
    public const string RetrievedSuccessfully = "Retrieved successfully";

    // Error Messages
    public const string BadRequest = "Bad Request - Invalid data provided";
    public const string Unauthorized = "Unauthorized - User not authenticated";
    public const string Forbidden = "Forbidden - User does not have the required permissions";
    public const string NotFound = "Not Found - Resource with the specified ID not found";
    public const string InternalServerError = "Internal Server Error - An error occurred while processing the request";
    public const string Conflict = "Conflict - A resource with the same unique key already exists";
}

