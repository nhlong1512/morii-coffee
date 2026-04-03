# Google Authentication in MoriiCoffee - Implementation Guide

**Status**: ✅ Implemented - 2026-03-28 (Branch: 005-google-oauth)
**Project**: MoriiCoffee Coffee Shop Management System
**Architecture**: Clean Architecture with .NET 8, ASP.NET Core Identity, MediatR

**Frontend Integration**: See [google-auth-integration-guide.md](./google-auth-integration-guide.md) for Next.js integration steps

---

## Analogy: The Coffee Shop Loyalty Card System

Think of Google Authentication like joining a coffee shop's loyalty program with your driver's license:

1. **You arrive at MoriiCoffee** (User clicks "Sign in with Google")
2. **The barista asks for your ID** (App redirects to Google)
3. **You show your driver's license** (You log into Google)
4. **The barista verifies it's real** (Google validates your credentials)
5. **They create your loyalty account** (Google sends back your profile info)
6. **MoriiCoffee creates your customer profile** (App creates/finds your account)
7. **You get a loyalty card** (App issues JWT access token)
8. **You get a backup card for later** (App issues refresh token)

The coffee shop doesn't need to know your password - they just trust that the government (Google) verified your identity.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   GOOGLE AUTHENTICATION FLOW (MORII COFFEE)              │
└─────────────────────────────────────────────────────────────────────────┘

   [User's Browser]           [MoriiCoffee API]              [Google OAuth]
         │                            │                              │
         │  1. POST /external-login   │                              │
         ├───────────────────────────>│                              │
         │     provider="Google"      │                              │
         │                            │                              │
         │  2. Challenge Response     │                              │
         │<───────────────────────────┤                              │
         │     (302 Redirect)         │                              │
         │                            │                              │
         │  3. Redirect to Google     │                              │
         ├────────────────────────────┼─────────────────────────────>│
         │                            │                              │
         │  4. User enters credentials│                              │
         │<───────────────────────────┼──────────────────────────────┤
         │     (Google Login Page)    │                              │
         │                            │                              │
         │  5. Authorization granted  │                              │
         ├────────────────────────────┼─────────────────────────────>│
         │                            │                              │
         │  6. Redirect with code     │                              │
         │<───────────────────────────┼──────────────────────────────┤
         │     to /external-auth-callback  │                              │
         │                            │                              │
         │  7. GET /external-auth-callback │                              │
         ├───────────────────────────>│                              │
         │                            │  8. Exchange code for token  │
         │                            ├─────────────────────────────>│
         │                            │                              │
         │                            │  9. Returns user profile     │
         │                            │<─────────────────────────────┤
         │                            │     (email, name, phone)     │
         │                            │                              │
         │                            │  10. Create/Find User        │
         │                            │  ┌──────────────────────┐   │
         │                            │  │  Check if email      │   │
         │                            │  │  exists in AspNetUsers│  │
         │                            │  │                      │   │
         │                            │  │  If new:             │   │
         │                            │  │  - Create user       │   │
         │                            │  │  - Assign CUSTOMER   │   │
         │                            │  │  - Send welcome email│   │
         │                            │  └──────────────────────┘   │
         │                            │                              │
         │                            │  11. Generate Tokens         │
         │                            │  ┌──────────────────────┐   │
         │                            │  │  JWT Access Token    │   │
         │                            │  │  (configured expiry) │   │
         │                            │  │                      │   │
         │                            │  │  Refresh Token       │   │
         │                            │  │  (stored in DB)      │   │
         │                            │  └──────────────────────┘   │
         │                            │                              │
         │  12. Redirect to returnUrl │                              │
         │<───────────────────────────┤                              │
         │     + AuthTokenHolder cookie│                             │
         │       (contains both tokens)│                             │
         │                            │                              │
```

---

## Implementation Plan

### Step 1: Install Required NuGet Packages

```bash
# Add Google Authentication provider
dotnet add source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj \
  package Microsoft.AspNetCore.Authentication.Google
```

---

### Step 2: Configure Google OAuth in appsettings.json

**Location**: `source/MoriiCoffee.Presentation/appsettings.json`

```json
{
  "Authentication": {
    "Google": {
      "ClientSecret": "",
      "ClientId": ""
    }
  },
  "JwtOptions": {
    "Secret": "",
    "Issuer": "",
    "Audience": "",
    "AccessTokenExpiryInMinutes": 480,
    "RefreshTokenExpiryInDays": 7
  },
}
```

**⚠️ Security Note**: Never commit secrets to source control. Use:
- **Development**: User secrets (`dotnet user-secrets`)
- **Production**: Environment variables or Azure Key Vault

---

### Step 3: Register Google Authentication in Dependency Injection

**Location**: `source/MoriiCoffee.Infrastructure/Configurations/AuthenticationConfiguration.cs` (create if doesn't exist)

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>Configures ASP.NET Core Identity and external authentication providers (Google).</summary>
public static class AuthenticationConfiguration
{
    public static IServiceCollection AddGoogleAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication()
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;

                // Request additional scopes
                options.Scope.Add("email");
                options.Scope.Add("profile");

                // Save tokens for later use
                options.SaveTokens = true;

                // Callback path (must match Google Cloud Console)
                options.CallbackPath = "/api/v1/auth/external-auth-callback";
            });

        return services;
    }
}
```

**Register in**: `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing services ...

    services.AddGoogleAuthentication(configuration);

    return services;
}
```

---

### Step 4: Create ExternalLogin Command

**Location**: `source/MoriiCoffee.Application/Commands/Auth/ExternalLogin/`

**ExternalLoginCommand.cs**:
```csharp
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLogin;

/// <summary>Command to initiate external OAuth login flow (Google, Facebook, etc.).</summary>
public class ExternalLoginCommand : ICommand<ExternalLoginResponseDto>
{
    /// <summary>OAuth provider name (e.g., "Google").</summary>
    public string Provider { get; set; } = null!;

    /// <summary>URL to redirect to after successful authentication.</summary>
    public string ReturnUrl { get; set; } = null!;
}
```

**ExternalLoginResponseDto.cs** (in `SeedWork/DTOs/Auth/`):
```csharp
using Microsoft.AspNetCore.Authentication;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>Response containing OAuth challenge properties for external login.</summary>
public class ExternalLoginResponseDto
{
    /// <summary>Authentication properties (redirect URL, state, nonce).</summary>
    public AuthenticationProperties Properties { get; set; } = null!;

    /// <summary>Provider name (e.g., "Google").</summary>
    public string Provider { get; set; } = null!;
}
```

**ExternalLoginCommandHandler.cs**:
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Domain.SeedWork.Command;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLogin;

/// <summary>Prepares OAuth challenge for external login (Google).</summary>
public class ExternalLoginCommandHandler : ICommandHandler<ExternalLoginCommand, ExternalLoginResponseDto>
{
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExternalLoginCommandHandler(
        SignInManager<UserEntity> signInManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _signInManager = signInManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<ExternalLoginResponseDto> Handle(
        ExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        // Build callback URL
        var httpContext = _httpContextAccessor.HttpContext!;
        var scheme = httpContext.Request.Scheme;
        var host = httpContext.Request.Host;
        var redirectUrl = $"{scheme}://{host}/api/v1/auth/external-auth-callback?returnUrl={Uri.EscapeDataString(request.ReturnUrl)}";

        // Configure OAuth properties
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            request.Provider,
            redirectUrl);
        properties.AllowRefresh = true;

        return Task.FromResult(new ExternalLoginResponseDto
        {
            Properties = properties,
            Provider = request.Provider
        });
    }
}
```

---

### Step 5: Create ExternalLoginCallback Command

**Location**: `source/MoriiCoffee.Application/Commands/Auth/ExternalLoginCallback/`

**ExternalLoginCallbackCommand.cs**:
```csharp
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;

/// <summary>Command to process OAuth callback after external authentication.</summary>
public class ExternalLoginCallbackCommand : ICommand<AuthResponseDto>
{
    /// <summary>URL to redirect to after processing.</summary>
    public string ReturnUrl { get; set; } = null!;
}
```

**ExternalLoginCallbackCommandHandler.cs**:
```csharp
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;

/// <summary>Processes Google OAuth callback: creates/finds user, assigns roles, generates tokens.</summary>
public class ExternalLoginCallbackCommandHandler : ICommandHandler<ExternalLoginCallbackCommand, AuthResponseDto>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public ExternalLoginCallbackCommandHandler(
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager,
        ITokenService tokenService,
        IEmailService emailService,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailService = emailService;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> Handle(
        ExternalLoginCallbackCommand request,
        CancellationToken cancellationToken)
    {
        // Get external login info populated by ASP.NET Identity middleware
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            throw new BadRequestException("Error loading external login information.");

        // Extract email from Google's claims
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
            throw new BadRequestException("Email not provided by external login provider.");

        // Check if user already exists
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            // NEW USER: Create from Google's data
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name);
            var userName = email.Split('@')[0]; // Use email prefix as username

            user = new UserEntity
            {
                UserName = userName,
                Email = email,
                PhoneNumber = info.Principal.FindFirstValue(ClaimTypes.MobilePhone),
                FullName = fullName,
                Status = EUserStatus.Active  // Auto-activate Google users
            };

            // Save to database
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new BadRequestException($"User creation failed: {errors}");
            }

            // Assign CUSTOMER role (all Google users are customers)
            await _userManager.AddToRoleAsync(user, nameof(ERole.CUSTOMER));

            // Send welcome email asynchronously
            _ = _emailService.SendWelcomeEmailAsync(user.Email!, user.UserName!);
        }

        // Link external login to user account (if not already linked)
        var existingLogin = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (existingLogin == null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
                throw new BadRequestException("Failed to link external login.");
        }

        // Sign in the user (creates ASP.NET Identity cookie)
        await _signInManager.SignInAsync(user, isPersistent: false);

        // Generate JWT access token
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);

        // Generate refresh token
        var refreshToken = Guid.NewGuid().ToString("N");
        await _userManager.SetAuthenticationTokenAsync(
            user,
            info.LoginProvider,
            TokenTypes.REFRESH,
            refreshToken);

        // Build response
        var roles = await _userManager.GetRolesAsync(user);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles.ToList();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = userDto
        };
    }
}
```

---

### Step 6: Add Controller Endpoints

**Location**: `source/MoriiCoffee.Presentation/Controllers/AuthController.cs`

```csharp
/// <summary>Initiate Google OAuth login flow.</summary>
[HttpPost("external-login")]
[SwaggerOperation(Summary = "External login", Description = "Initiates OAuth flow for external providers (Google). Redirects to provider login page.")]
[SwaggerResponse(302, "Redirect to external provider")]
[SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
public async Task<IActionResult> ExternalLogin(
    [FromQuery] string provider,
    [FromQuery] string returnUrl = "/")
{
    // Clear existing authentication
    if (User.Identity?.IsAuthenticated == true)
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }

    // Get OAuth challenge
    var command = new ExternalLoginCommand
    {
        Provider = provider,
        ReturnUrl = returnUrl
    };
    var result = await _mediator.Send(command);

    // Trigger browser redirect to Google
    return Challenge(result.Properties, result.Provider);
}

/// <summary>Handle OAuth callback from Google.</summary>
[HttpGet("external-auth-callback")]
[SwaggerOperation(Summary = "External callback", Description = "Processes OAuth callback from external providers. Creates/logs in user and returns tokens in cookie.")]
[SwaggerResponse(302, "Redirect to return URL with auth cookie")]
[SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
public async Task<IActionResult> ExternalCallback([FromQuery] string returnUrl = "/")
{
    // Process callback and get tokens
    var command = new ExternalLoginCallbackCommand { ReturnUrl = returnUrl };
    var authResponse = await _mediator.Send(command);

    // Store tokens in secure cookie (5-minute expiration)
    var cookieOptions = new CookieOptions
    {
        Expires = DateTime.UtcNow.AddMinutes(5),  // Short-lived
        HttpOnly = true,                          // Not accessible via JavaScript (XSS protection)
        Secure = true,                            // Only sent over HTTPS
        SameSite = SameSiteMode.Lax              // CSRF protection
    };

    // Serialize tokens to JSON with camelCase formatting
    var tokensJson = JsonSerializer.Serialize(authResponse, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    Response.Cookies.Append("AuthTokenHolder", tokensJson, cookieOptions);

    // Redirect back to frontend
    return Redirect(returnUrl);
}
```

---

## Database Schema

### AspNetUsers Table (ASP.NET Core Identity)

The User entity extends `IdentityUser<Guid>`:

```csharp
/// <summary>MoriiCoffee user. Extends IdentityUser and implements IAggregateRoot.</summary>
public class User : IdentityUser<Guid>, IAggregateRoot, IEntityBase
{
    public string? FullName { get; set; }
    public DateTime? Dob { get; set; }
    public EGender? Gender { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarFileName { get; set; }
    public EUserStatus Status { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### AspNetUserLogins Table (External Login Associations)

```sql
CREATE TABLE AspNetUserLogins (
    LoginProvider NVARCHAR(128),     -- "Google"
    ProviderKey NVARCHAR(128),        -- Google's user ID
    ProviderDisplayName NVARCHAR(MAX),-- "Google"
    UserId UNIQUEIDENTIFIER,          -- MoriiCoffee user ID
    PRIMARY KEY (LoginProvider, ProviderKey)
);
```

### AspNetUserTokens Table (Refresh Tokens)

```sql
CREATE TABLE AspNetUserTokens (
    UserId UNIQUEIDENTIFIER,
    LoginProvider NVARCHAR(128),      -- "Google"
    Name NVARCHAR(128),                -- "REFRESH"
    Value NVARCHAR(MAX),               -- The actual token string
    PRIMARY KEY (UserId, LoginProvider, Name)
);
```

---

## Google Cloud Console Configuration

### 1. Create OAuth 2.0 Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project (or select existing)
3. Enable "Google+ API" or "People API"
4. Navigate to **Credentials** → **Create Credentials** → **OAuth 2.0 Client ID**
5. Choose **Web application**
6. Configure:

**Authorized JavaScript origins**:
```
http://localhost:8002
https://morii-coffee.com
```

**Authorized redirect URIs**:
```
http://localhost:8002/api/v1/auth/external-auth-callback
https://morii-coffee.com/api/v1/auth/external-auth-callback
```

7. Copy **Client ID** and **Client Secret**

### 2. Configure User Secrets (Development)

```bash
cd source/MoriiCoffee.Presentation

dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"
```

---

## Security Features

### 1. State Parameter (CSRF Protection)

```
Google login URL includes:
?state=CfDJ8Abc123...

When Google redirects back, state must match.
Prevents attackers from forging callbacks.
```

### 2. Nonce Parameter (Replay Attack Prevention)

Ensures each login attempt is unique. Token can't be reused even if intercepted.

### 3. HTTPS Only

```csharp
cookieOptions.Secure = true;  // Cookie only sent over HTTPS
```

### 4. HttpOnly Cookies

```csharp
cookieOptions.HttpOnly = true;  // JavaScript can't access cookie
```

### 5. Token Expiration

- **Access Token**: Configured in `JwtOptions.AccessTokenExpiryInMinutes`
- **Refresh Token**: No expiration, but stored in DB (can be revoked)
- **Auth Cookie**: 5 minutes (just for passing tokens to frontend)

### 6. Role-Based Authorization

```csharp
[Authorize(Roles = "CUSTOMER")]
public async Task<IActionResult> PlaceOrder() { ... }
```

---

## Common Gotchas

### Gotcha #1: "Redirect URI mismatch"

**Problem**:
```
Error 400: redirect_uri_mismatch
The redirect URI in the request does not match the authorized redirect URIs.
```

**Solution**:
Ensure Google Cloud Console has EXACT redirect URI:
```
http://localhost:8002/api/v1/auth/external-auth-callback
```
No trailing slashes. Must match EXACTLY.

---

### Gotcha #2: "The state parameter is invalid"

**Problem**:
```
InvalidOperationException: The state parameter in the URL doesn't match the state in the cookie.
```

**Why it happens**:
- User bookmarked the callback URL
- User refreshed during OAuth flow
- Cookie expired before OAuth completed

**Solution**:
Start the flow again from `/external-login`. State is one-time use.

---

### Gotcha #3: Email Not Provided

**Problem**:
```csharp
if (string.IsNullOrEmpty(email))
    throw new BadRequestException("Email not provided");
```

**Solution**:
Ensure scopes are requested:
```csharp
googleOptions.Scope.Add("email");
googleOptions.Scope.Add("profile");
```

---

## Testing Checklist

### Manual Testing

- [ ] Click "Sign in with Google" redirects to Google
- [ ] After Google login, redirected back to app
- [ ] New user created with email from Google
- [ ] New user assigned CUSTOMER role
- [ ] New user receives welcome email
- [ ] Access token works for authenticated endpoints
- [ ] Refresh token can get new access token
- [ ] Existing user logs in (doesn't create duplicate)
- [ ] User profile shows Google data (name, email)

### Security Testing

- [ ] Token can't be forged (invalid signature rejected)
- [ ] Expired token rejected with 401
- [ ] Callback with wrong state parameter fails
- [ ] Access token doesn't contain passwords
- [ ] HTTPS enforced on cookie

### Edge Cases

- [ ] User denies permission on Google page
- [ ] Google account has no email
- [ ] Multiple rapid login attempts
- [ ] Login with expired Google session

---

## Troubleshooting Commands

### Check if user exists

```sql
SELECT * FROM AspNetUsers WHERE Email = 'user@gmail.com';
```

### Check external logins

```sql
SELECT u.Email, ul.LoginProvider, ul.ProviderKey
FROM AspNetUsers u
JOIN AspNetUserLogins ul ON u.Id = ul.UserId
WHERE u.Email = 'user@gmail.com';
```

### Check refresh tokens

```sql
SELECT u.Email, ut.LoginProvider, ut.Name, ut.Value
FROM AspNetUsers u
JOIN AspNetUserTokens ut ON u.Id = ut.UserId
WHERE ut.LoginProvider = 'Google';
```

### Decode JWT Token

Use [jwt.io](https://jwt.io) or:
```bash
echo "your.jwt.token" | cut -d'.' -f2 | base64 -d | jq
```

---

## Summary

The MoriiCoffee Google authentication flow uses **OAuth 2.0 Authorization Code Flow** with these components:

1. **ASP.NET Identity** - Handles OAuth mechanics (redirects, token exchange, claims)
2. **Google OAuth Provider** - External identity verification
3. **JWT Tokens** - Stateless authentication for API requests
4. **Refresh Tokens** - Long-lived tokens for getting new access tokens
5. **Clean Architecture** - Separation: Controllers → Commands → Handlers → Services → Domain
6. **MediatR Pattern** - Commands (ExternalLogin, ExternalLoginCallback)

**Key files to create**:
- `Configurations/AuthenticationConfiguration.cs` - OAuth setup
- `Commands/Auth/ExternalLogin/` - Initiates OAuth flow
- `Commands/Auth/ExternalLoginCallback/` - Processes callback
- `Controllers/AuthController.cs` - Add external-login and external-auth-callback endpoints

**Token lifecycle**:
```
Google Login → Access Token (configurable) + Refresh Token (∞)
Access Token expires → Use Refresh Token → Get new Access Token
Refresh Token compromised → Revoke in database → Force re-login
```

The system follows modern authentication best practices and integrates seamlessly with MoriiCoffee's existing Clean Architecture structure.
