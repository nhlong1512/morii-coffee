using System.Security.Claims;
using MoriiCoffee.Domain.Aggregates.UserAggregate;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>Service for generating and validating JWT access tokens and opaque refresh tokens.</summary>
public interface ITokenService
{
    /// <summary>
    /// Asynchronously generates an access token for the specified user.
    /// </summary>
    /// <param name="user">
    /// The user for whom the access token is being generated. This user object typically contains user information 
    /// such as claims and roles.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a string representing the generated access token.
    /// </returns>

    Task<string> GenerateAccessTokenAsync(User user);

    /// <summary>
    /// Extracts the claims principal from the provided token.
    /// </summary>
    /// <param name="token">
    /// The JWT token from which to extract the claims principal. This token contains the claims associated with the user.
    /// </param>
    /// <returns>
    /// A <see cref="ClaimsIdentity"/> object representing the claims extracted from the token, or null if the token is invalid or cannot be parsed.
    /// </returns>
    Task<ClaimsIdentity?> GetPrincipalFromTokenAsync(string token);
}
