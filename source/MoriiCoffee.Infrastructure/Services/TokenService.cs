using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Shared.Settings;
using Serilog;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>JWT token service. Generates HS256-signed access tokens with email, userId (jti), and role claims. Validates tokens with lifetime check disabled for the refresh flow.</summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly UserManager<User> _userManager;

    private readonly ILogger _logger;

    private readonly JsonWebTokenHandler _handler = new();

    public TokenService(JwtOptions jwtOptions, UserManager<User> userManager, ILogger logger)
    {
        _jwtOptions = jwtOptions;
        _userManager = userManager;
        _logger = logger;

    }

    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryInMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return _handler.CreateToken(descriptor);
    }

    public async Task<ClaimsIdentity?> GetPrincipalFromTokenAsync(string token)
    {
        byte[] key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidAudience = _jwtOptions.Audience,
            ValidIssuer = _jwtOptions.Issuer,
        };

        TokenValidationResult result = await _handler.ValidateTokenAsync(token, tokenValidationParameters);

        if (result.Exception is not null)
        {
            _logger.Error(result.Exception.Message);
            return null;
        }

        return result.ClaimsIdentity;
    }
}
