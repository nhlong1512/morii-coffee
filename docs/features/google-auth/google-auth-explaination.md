# Google Authentication in EventHub - Complete Explanation

## Analogy: The Hotel Check-In System

Think of Google Authentication like checking into a hotel with your driver's license:

1. **You arrive at the hotel** (User clicks "Sign in with Google")
2. **The receptionist asks for your ID** (App redirects to Google)
3. **You show your driver's license** (You log into Google)
4. **The receptionist verifies it's real** (Google validates your credentials)
5. **They give you a room key** (Google sends back your profile info)
6. **The hotel creates your guest record** (App creates/finds your account)
7. **You get a keycard for the week** (App issues JWT access token)
8. **You get a spare key for later** (App issues refresh token)

The hotel doesn't need to know your password - they just trust that the government (Google) verified your identity.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          GOOGLE AUTHENTICATION FLOW                      │
└─────────────────────────────────────────────────────────────────────────┘

   [User's Browser]              [EventHub Server]              [Google OAuth]
         │                              │                              │
         │  1. POST /external-login     │                              │
         ├─────────────────────────────>│                              │
         │     provider="Google"        │                              │
         │                              │                              │
         │  2. Challenge Response       │                              │
         │<─────────────────────────────┤                              │
         │     (302 Redirect)           │                              │
         │                              │                              │
         │  3. Redirect to Google Login │                              │
         ├──────────────────────────────┼─────────────────────────────>│
         │                              │                              │
         │  4. User enters credentials  │                              │
         │<─────────────────────────────┼──────────────────────────────┤
         │     (Google Login Page)      │                              │
         │                              │                              │
         │  5. Authorization granted    │                              │
         ├──────────────────────────────┼─────────────────────────────>│
         │                              │                              │
         │  6. Redirect with auth code  │                              │
         │<─────────────────────────────┼──────────────────────────────┤
         │     to /external-auth-callback│                             │
         │                              │                              │
         │  7. GET /external-auth-callback                             │
         ├─────────────────────────────>│                              │
         │                              │  8. Exchange code for token  │
         │                              ├─────────────────────────────>│
         │                              │                              │
         │                              │  9. Returns user profile     │
         │                              │<─────────────────────────────┤
         │                              │     (email, name, phone)     │
         │                              │                              │
         │                              │  10. Create/Find User        │
         │                              │  ┌──────────────────────┐   │
         │                              │  │  Check if email      │   │
         │                              │  │  exists in DB        │   │
         │                              │  │                      │   │
         │                              │  │  If new:             │   │
         │                              │  │  - Create user       │   │
         │                              │  │  - Assign roles      │   │
         │                              │  │  - Send email        │   │
         │                              │  └──────────────────────┘   │
         │                              │                              │
         │                              │  11. Generate Tokens         │
         │                              │  ┌──────────────────────┐   │
         │                              │  │  JWT Access Token    │   │
         │                              │  │  (8-hour expiry)     │   │
         │                              │  │                      │   │
         │                              │  │  Refresh Token       │   │
         │                              │  │  (stored in DB)      │   │
         │                              │  └──────────────────────┘   │
         │                              │                              │
         │  12. Redirect to returnUrl   │                              │
         │<─────────────────────────────┤                              │
         │     + AuthTokenHolder cookie │                              │
         │       (contains both tokens) │                              │
         │                              │                              │
```

---

## Step-by-Step Code Walkthrough

### Step 1: User Initiates Google Login

**Location**: `source/EventHub.Presentation/Controllers/AuthController.cs:50-63`

```csharp
[HttpPost("external-login")]
public async Task<IActionResult> ExternalLogin(string provider, Uri returnUrl)
{
    // Clean slate: sign out if already logged in
    if (User.Identity != null)
    {
        await _mediator.Send(new SignOutCommand());
    }

    // Ask the handler to prepare the Google OAuth challenge
    ExternalLoginDto externalLoginResponse =
        await _mediator.Send(new ExternalLoginCommand(provider, returnUrl));

    // Challenge browser to redirect to Google
    return Challenge(externalLoginResponse.Properties, externalLoginResponse.Provider);
}
```

**What happens:**
- Frontend calls `POST /api/v1/auth/external-login?provider=Google&returnUrl=https://myapp.com/home`
- Server signs out any existing session (clean slate)
- Creates a "challenge" response - ASP.NET Core's way of saying "redirect user to external login"
- Browser automatically redirects to Google's login page

---

### Step 2: Prepare the OAuth Challenge

**Location**: `source/EventHub.Application/Commands/Auth/ExternalLogin/ExternalLoginCommandHandler.cs`

```csharp
public async Task<ExternalLoginDto> Handle(ExternalLoginCommand request,
    CancellationToken cancellationToken)
{
    // Get current HTTP context to build URLs
    HttpContext httpContext = _signInManager.Context;
    string domainName = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

    // Build the callback URL Google will redirect to after login
    string redirectUrl = $"{domainName}/api/v1/auth/external-auth-callback?returnUrl={request.ReturnUrl}";

    // Let ASP.NET Identity configure OAuth properties for Google
    AuthenticationProperties properties =
        _signInManager.ConfigureExternalAuthenticationProperties(request.Provider, redirectUrl);
    properties.AllowRefresh = true;

    return new ExternalLoginDto
    {
        Properties = properties,  // Contains redirect URL, state, nonce, etc.
        Provider = request.Provider  // "Google"
    };
}
```

**What happens:**
- Constructs the callback URL: `https://yourserver.com/api/v1/auth/external-auth-callback?returnUrl=...`
- ASP.NET Identity automatically adds OAuth parameters (state, nonce for security)
- Returns authentication properties that trigger the browser redirect

---

### Step 3: User Logs Into Google

(This happens on Google's servers - not in our code)

**What happens:**
- User sees Google's login page
- User enters email/password or uses saved session
- Google asks: "EventHub wants to access your profile. Allow?"
- User clicks "Allow"

---

### Step 4: Google Redirects Back with Authorization Code

Google redirects to: `https://yourserver.com/api/v1/auth/external-auth-callback?code=ABC123&state=XYZ`

ASP.NET Identity middleware automatically:
- Validates the `state` parameter (prevents CSRF attacks)
- Exchanges the `code` for an access token by calling Google's token endpoint
- Retrieves user profile from Google
- Stores this info in `ExternalLoginInfo`

---

### Step 5: Process the Callback and Create/Login User

**Location**: `source/EventHub.Application/Commands/Auth/ExternalLoginCallback/ExternalLoginCallbackCommandHandler.cs`

```csharp
public async Task<SignInResponseDto> Handle(ExternalLoginCallbackCommand request,
    CancellationToken cancellationToken)
{
    // 1. Get the external login info populated by ASP.NET Identity middleware
    ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();

    // 2. Extract email from Google's claims
    string email = info?.Principal.FindFirstValue(ClaimTypes.Email);

    if (!string.IsNullOrEmpty(email))
    {
        // 3. Check if user already exists in our database
        User user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            // 4. NEW USER: Create from Google's data
            user = new User
            {
                UserName = email,  // Use email as username
                Email = email,
                PhoneNumber = info!.Principal.FindFirstValue(ClaimTypes.MobilePhone),
                FullName = info.Principal.FindFirstValue(ClaimTypes.Name),
                Status = EUserStatus.ACTIVE  // Auto-activate Google users
            };

            // 5. Save to database
            IdentityResult result = await _userManager.CreateAsync(user);

            if (result.Succeeded)
            {
                // 6. Assign default roles (every Google user gets these)
                await _userManager.AddToRolesAsync(user, new List<string>
                {
                    nameof(EUserRole.CUSTOMER),   // Can buy tickets
                    nameof(EUserRole.ORGANIZER)   // Can create events
                });

                // 7. Send welcome email asynchronously (via Hangfire)
                _hangfireService.Enqueue(() =>
                    _emailService.SendRegistrationConfirmationEmailAsync(
                        user.Email, user.UserName).Wait());
            }
        }

        // 8. Sign in the user (creates ASP.NET Identity cookie)
        await _signInManager.SignInAsync(user, false);

        // 9. Generate our custom JWT access token
        string accessToken = await _tokenService.GenerateAccessTokenAsync(user);

        // 10. Generate refresh token
        string refreshToken = await _userManager.GenerateUserTokenAsync(
            user, info!.LoginProvider, TokenTypes.REFRESH);

        // 11. Store refresh token in database (aspnetUserTokens table)
        await _userManager.SetAuthenticationTokenAsync(
            user, info.LoginProvider, TokenTypes.REFRESH, refreshToken);

        // 12. Return both tokens
        return new SignInResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    throw new BadRequestException("Failed to sign in");
}
```

**What happens:**
- **Existing users**: Just log them in and issue new tokens
- **New users**: Create account, assign roles (CUSTOMER + ORGANIZER), send welcome email
- All users get two tokens:
  - **Access Token** (JWT) - short-lived (8 hours), sent with every API request
  - **Refresh Token** - long-lived, used to get new access tokens when they expire

---

### Step 6: Generate JWT Access Token

**Location**: `source/EventHub.Infrastructure/Services/TokenService.cs:15-55`

```csharp
public async Task<string> GenerateAccessTokenAsync(User user)
{
    var tokenHandler = new JsonWebTokenHandler();
    byte[] key = Encoding.ASCII.GetBytes(_jwtOptions.Secret);

    // 1. Get user's roles from database
    IList<string> roles = await _userManager.GetRolesAsync(user);

    // 2. Get all permissions for those roles (function + command combinations)
    IQueryable<string> query = from p in _context.Permissions
                               join c in _context.Commands on p.CommandId equals c.Id
                               join f in _context.Functions on p.FunctionId equals f.Id
                               join r in _roleManager.Roles on p.RoleId equals r.Id
                               where roles.Contains(r.Name ?? "")
                               select f.Id + "_" + c.Id;
    List<string> permissions = await query.Distinct().ToListAsync();

    // 3. Build claims (data embedded in the token)
    var claimList = new List<Claim>
    {
        new(ClaimTypes.Email, user.Email ?? ""),
        new(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""),
        new(JwtRegisteredClaimNames.Jti, user.Id.ToString()),  // User ID
        new(ClaimTypes.Role, string.Join(";", roles)),         // "CUSTOMER;ORGANIZER"
        new(SystemConstants.Claims.Permissions,
            JsonConvert.SerializeObject(permissions))          // Serialized permission list
    };

    // 4. Create token descriptor
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Issuer = _jwtOptions.Issuer,
        Audience = _jwtOptions.Audience,
        Subject = new ClaimsIdentity(claimList),
        Expires = DateTime.UtcNow.AddHours(8),  // 8-hour expiration
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256)      // HMAC-SHA256 signature
    };

    // 5. Generate the JWT string
    return tokenHandler.CreateToken(tokenDescriptor);
}
```

**What's in the JWT:**
```json
{
  "email": "user@gmail.com",
  "phone": "+1234567890",
  "jti": "12345-67890-abcdef",
  "role": "CUSTOMER;ORGANIZER",
  "permissions": "[\"EVENT_CREATE\",\"TICKET_BUY\",...]",
  "exp": 1743532800,
  "iss": "EventHub",
  "aud": "EventHub.Users"
}
```

**Token Security:**
- Signed with HMAC-SHA256 (prevents tampering)
- Contains user ID, roles, and permissions (no need to query DB on every request)
- 8-hour expiration (balance between security and convenience)
- Can be validated without database lookup (stateless)

---

### Step 7: Return Tokens to Frontend

**Location**: `source/EventHub.Presentation/Controllers/AuthController.cs:72-89`

```csharp
[HttpGet("external-auth-callback")]
public async Task<IActionResult> ExternalLoginCallback([FromQuery] Uri returnUrl)
{
    // Get both tokens from the handler
    SignInResponseDto signInResponse =
        await _mediator.Send(new ExternalLoginCallbackCommand(returnUrl));

    // Store tokens in a secure cookie (5-minute expiration)
    var options = new CookieOptions
    {
        Expires = DateTime.UtcNow.AddMinutes(5),  // Short-lived
        HttpOnly = true,                          // Not accessible via JavaScript (XSS protection)
        Secure = true                             // Only sent over HTTPS
    };

    // Serialize tokens to JSON with camelCase formatting
    Response.Cookies.Append("AuthTokenHolder",
        JsonConvert.SerializeObject(signInResponse, new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        }), options);

    // Redirect back to the frontend (e.g., https://myapp.com/home)
    return Redirect(returnUrl.ToString());
}
```

**What happens:**
- Tokens stored in cookie named `AuthTokenHolder`
- Cookie expires in 5 minutes (just enough time for frontend to extract and store them)
- Frontend JavaScript reads the cookie and saves tokens to localStorage/sessionStorage
- Browser redirects to original `returnUrl` (e.g., dashboard page)

---

## Configuration Required

### Google Cloud Console Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project
3. Enable "Google+ API"
4. Create OAuth 2.0 credentials:
   - **Authorized JavaScript origins**: `https://yourserver.com`
   - **Authorized redirect URIs**: `https://yourserver.com/api/v1/auth/external-auth-callback`
5. Copy Client ID and Client Secret

### appsettings.json Configuration

**Location**: `source/EventHub.Presentation/appsettings.json`

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "123456789-abcdefg.apps.googleusercontent.com",
      "ClientSecret": "GOCSPX-your-secret-key-here"
    }
  },
  "JwtOptions": {
    "Secret": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "EventHub",
    "Audience": "EventHub.Users"
  }
}
```

---

## Database Schema

### Users Table
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserName NVARCHAR(256),
    Email NVARCHAR(256),
    PhoneNumber NVARCHAR(MAX),
    FullName NVARCHAR(MAX),
    Status INT  -- 0=Inactive, 1=Active, 2=Banned
);
```

### User Roles Table
```sql
CREATE TABLE UserRoles (
    UserId UNIQUEIDENTIFIER,
    RoleId UNIQUEIDENTIFIER,
    PRIMARY KEY (UserId, RoleId)
);
```

### User Tokens Table (Refresh Tokens)
```sql
CREATE TABLE aspnetUserTokens (
    UserId UNIQUEIDENTIFIER,
    LoginProvider NVARCHAR(128),  -- "Google"
    Name NVARCHAR(128),            -- "REFRESH"
    Value NVARCHAR(MAX),           -- The actual token string
    PRIMARY KEY (UserId, LoginProvider, Name)
);
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

### 2. Nonce Parameter (Replay Attack Protection)
```
Ensures each login attempt is unique.
Token can't be reused even if intercepted.
```

### 3. HTTPS Only
```csharp
cookieOptions.Secure = true;  // Cookie only sent over HTTPS
```

### 4. HttpOnly Cookies
```csharp
cookieOptions.HttpOnly = true;  // JavaScript can't access cookie
```

### 5. Token Expiration
- **Access Token**: 8 hours (can't be revoked, so keep it short)
- **Refresh Token**: No expiration, but stored in DB (can be revoked)
- **Auth Cookie**: 5 minutes (just for passing tokens to frontend)

### 6. Role-Based Authorization
```csharp
[Authorize(Roles = "ORGANIZER")]
public async Task<IActionResult> CreateEvent() { ... }
```

### 7. Permission-Based Authorization
```csharp
[Authorize(Policy = "EVENT_CREATE")]
public async Task<IActionResult> CreateEvent() { ... }
```

---

## Common Gotchas

### Gotcha #1: "The state parameter is invalid"

**Problem:**
```
InvalidOperationException: The state parameter in the URL doesn't match the state in the cookie.
```

**Why it happens:**
- User bookmarked the Google callback URL
- User refreshed the page during OAuth flow
- Cookie expired before OAuth completed

**Solution:**
Start the flow again from `/external-login`. The state token is one-time use only.

---

### Gotcha #2: "Redirect URI mismatch"

**Problem:**
```
Error 400: redirect_uri_mismatch
The redirect URI in the request: https://yourserver.com/api/v1/auth/external-auth-callback
does not match the ones authorized for the OAuth client.
```

**Why it happens:**
Google Cloud Console has different redirect URIs configured.

**Solution:**
Go to Google Cloud Console → Credentials → Edit OAuth 2.0 Client → Add exact redirect URI:
```
https://yourserver.com/api/v1/auth/external-auth-callback
```
No trailing slashes. Must match EXACTLY.

---

### Gotcha #3: Token Refresh vs. New Login

**Problem:**
User's access token expires after 8 hours. What happens?

**Solution:**
Frontend should detect 401 responses and use refresh token:

```javascript
// When access token expires
async function refreshAccessToken() {
    const response = await fetch('/api/v1/auth/refresh-token', {
        method: 'POST',
        body: JSON.stringify({ refreshToken: currentRefreshToken })
    });

    const { accessToken } = await response.json();
    // Update stored access token
}
```

**Note:** The codebase has a refresh token endpoint, but it's separate from Google OAuth.

---

### Gotcha #4: Multiple Google Accounts

**Problem:**
User has multiple Google accounts. Which one gets used?

**Solution:**
Google shows account chooser if multiple accounts logged in. User picks which one.

**Edge case:**
If user logs in with `personal@gmail.com` first, then tries `work@gmail.com`:
- EventHub checks email in database
- Finds existing account with `personal@gmail.com`
- Logs in with that account (ignores `work@gmail.com`)

To use different account, user must register a separate EventHub account.

---

### Gotcha #5: Email Not Provided by Google

**Problem:**
```csharp
string email = info?.Principal.FindFirstValue(ClaimTypes.Email);
if (string.IsNullOrEmpty(email)) {
    throw new BadRequestException("Failed to sign in");
}
```

**Why it happens:**
User's Google account doesn't have verified email.

**Solution:**
Add more scopes in configuration:
```csharp
googleOptions.Scope.Add("email");  // Explicitly request email scope
googleOptions.Scope.Add("profile");
```

---

### Gotcha #6: Token Size

**Problem:**
JWT tokens can get large with many permissions:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6InVzZXJAZ21haWwuY29tIiwi...
(3000+ characters)
```

**Why it matters:**
- HTTP header size limits (8KB in most servers)
- Bandwidth on mobile devices
- Cookie size limits

**Solution:**
- Store permissions by reference (permission IDs instead of full strings)
- Use shorter claim names (`uid` instead of `ClaimTypes.Email`)
- Consider storing permissions in database and querying on each request (trade-off: speed vs. size)

---

## Testing Checklist

### Manual Testing

- [ ] Click "Sign in with Google" redirects to Google
- [ ] After Google login, redirected back to app
- [ ] New user created with email from Google
- [ ] New user assigned CUSTOMER and ORGANIZER roles
- [ ] New user receives welcome email
- [ ] Access token works for authenticated endpoints
- [ ] Access token expires after 8 hours
- [ ] Refresh token can get new access token
- [ ] Existing user logs in (doesn't create duplicate)
- [ ] User profile shows Google data (name, email)

### Security Testing

- [ ] Token can't be forged (invalid signature rejected)
- [ ] Expired token rejected with 401
- [ ] Refresh token can't be used as access token
- [ ] Callback with wrong state parameter fails
- [ ] Callback without code parameter fails
- [ ] Access token doesn't contain passwords or secrets
- [ ] Tokens not logged in server logs
- [ ] HTTPS enforced on cookie

### Edge Cases

- [ ] User denies permission on Google page
- [ ] Google account has no email
- [ ] User tries to use same email with different provider
- [ ] Multiple rapid login attempts
- [ ] Login during server restart
- [ ] Login with expired Google session

---

## Troubleshooting Commands

### Check if user exists
```sql
SELECT * FROM Users WHERE Email = 'user@gmail.com';
```

### Check user roles
```sql
SELECT u.Email, r.Name
FROM Users u
JOIN UserRoles ur ON u.Id = ur.UserId
JOIN Roles r ON ur.RoleId = r.Id
WHERE u.Email = 'user@gmail.com';
```

### Check refresh tokens
```sql
SELECT UserId, LoginProvider, Name, Value
FROM aspnetUserTokens
WHERE LoginProvider = 'Google';
```

### Decode JWT Token
Use [jwt.io](https://jwt.io) or:
```bash
echo "your.jwt.token" | cut -d'.' -f2 | base64 -d | jq
```

### View ASP.NET Identity logs
```csharp
// In Startup.cs
services.AddLogging(builder =>
{
    builder.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
});
```

---

## Summary

The EventHub Google authentication flow uses **OAuth 2.0 Authorization Code Flow** with these key components:

1. **ASP.NET Identity** - Handles OAuth mechanics (redirects, token exchange, claims)
2. **Google OAuth Provider** - External identity verification
3. **JWT Tokens** - Stateless authentication for API requests
4. **Refresh Tokens** - Long-lived tokens for getting new access tokens
5. **CQRS Pattern** - Commands (ExternalLogin, ExternalLoginCallback) handle flow
6. **Clean Architecture** - Separation of concerns (Controllers → Handlers → Services → Domain)

**Key files:**
- **Controllers/AuthController.cs** - Endpoints
- **Commands/Auth/ExternalLogin** - Initiates OAuth flow
- **Commands/Auth/ExternalLoginCallback** - Processes callback
- **Services/TokenService.cs** - JWT generation
- **Configurations/AuthenticationConfiguration.cs** - OAuth setup

**Token lifecycle:**
```
Google Login → Access Token (8h) + Refresh Token (∞)
Access Token expires → Use Refresh Token → Get new Access Token (8h)
Refresh Token compromised → Revoke in database → Force re-login
```

The system is secure, scalable, and follows modern authentication best practices.
