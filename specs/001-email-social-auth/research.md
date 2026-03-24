# Research: Email Integration and OAuth2 Social Login

**Feature**: Email Integration and Social Login Planning
**Date**: 2026-03-23
**Reference**: Full research document at `/docs/oauth-email-best-practices.md`

This document consolidates research findings and design decisions for implementing SendGrid transactional emails and planning OAuth2 social login with Google and Meta.

---

## 1. SendGrid Email Best Practices

### Decision: Use SendGrid SDK with Fire-and-Forget Pattern

**Rationale**:
- SendGrid provides 99.99% uptime SLA with official .NET SDK support
- Handles 10,000 requests/second with deliverability features (SPF/DM ARC, DKIM)
- Fire-and-forget pattern prevents email failures from blocking user operations (signup, password reset)
- Existing infrastructure from Phase 2 can be leveraged (IEmailService, SendGridEmailService)

**Alternatives Considered**:
- **Resend**: Modern developer experience but less mature than SendGrid (rejected: lacks track record)
- **SMTP relay**: Lower-level control but requires manual infrastructure management (rejected: operational overhead)
- **Amazon SES**: Cost-effective but requires more setup for deliverability monitoring (rejected: complexity)

**Implementation Notes**:
- Use SendGrid .NET SDK (`SendGrid` NuGet package)
- Retry logic with exponential backoff for 429 rate limit errors (1s, 3s, 5s delays)
- Log all send attempts with structured properties (recipient, template, status, error)
- Do not throw exceptions on email send failure; log and continue (graceful degradation)
- Rate limits: Free tier = 100 emails/day; Essentials plan = 100,000 emails/month

**Error Handling Strategy**:
```csharp
public async Task SendWelcomeEmailAsync(string to, string name)
{
    try
    {
        var msg = new SendGridMessage();
        msg.SetFrom(new EmailAddress(_settings.FromEmail, _settings.FromName));
        msg.AddTo(new EmailAddress(to));
        msg.SetSubject("Welcome to Morii Coffee!");
        msg.AddContent(MimeType.Html, EmailTemplates.WelcomeEmail(name, storefrontUrl));

        var response = await _client.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
            _logger.LogInformation("Welcome email sent to {Email}", to);
        else
            _logger.LogWarning("Welcome email failed: {StatusCode}", response.StatusCode);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "SendGrid exception sending welcome email to {Email}", to);
        // Do not rethrow - graceful degradation
    }
}
```

---

## 2. HTML Email Template Design

### Decision: Table-based Layout with Inline CSS (Hex Colors)

**Rationale**:
- Gmail strips `<head>` styles entirely; inline CSS is the only reliable approach
- Outlook (Windows) uses Microsoft Word rendering engine, requiring table-based layouts
- OKLCH colors (used in frontend) are not supported by email clients; hex colors are universally safe
- 600px max width ensures compatibility across desktop and mobile clients

**Alternatives Considered**:
- **CSS Grid/Flexbox**: Modern but unsupported in Outlook and many email clients (rejected: compatibility)
- **`<div>`-based layouts**: Unreliable across Outlook versions (rejected: rendering issues)
- **External stylesheets**: Stripped by Gmail (rejected: not supported)

**Implementation Notes**:

**Safe CSS Properties**:
- ✅ `padding`, `margin`, `background-color` (hex), `color` (hex), `font-family` (with fallbacks), `font-size`, `font-weight`, `line-height`, `text-align`, `border`, `border-radius` (modern clients), `width`, `max-width`

**Unsafe CSS Properties** (avoid):
- ❌ `position`, `float`, `display: flex`, `display: grid`, `background: linear-gradient()`, `box-shadow` (unreliable)

**Brand Color Conversion**:
- Frontend uses OKLCH: `oklch(60% 0.15 30)`
- Convert to hex for email: `#8B4513` (brown/coffee primary)
- Use https://oklch.net/oklch-to-hex for conversion

**Email Client Compatibility**:
| Feature | Gmail | Outlook (Win) | Apple Mail | Yahoo |
|---------|-------|---------------|------------|-------|
| Inline CSS | ✓ | ✓ | ✓ | ✓ |
| `<head>` styles | ✗ | ✓ | ✓ | ✓ |
| Table layouts | ✓ | ✓ | ✓ | ✓ |
| Flexbox/Grid | ✗ | ✗ | Partial | ✗ |

**Plain-Text Fallback**:
- Always include plain-text version with `msg.AddContent(MimeType.Text, plainTextBody)`
- Prevents spam filter triggers
- Accessible for screen readers

**Template Structure** (abbreviated):
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="margin: 0; padding: 0; background-color: #f5f5f5; font-family: Arial, Helvetica, sans-serif;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="max-width: 600px; width: 100%; background-color: #ffffff;">
                    <!-- Header with logo -->
                    <tr>
                        <td style="padding: 40px 30px; text-align: center; background-color: #8B4513;">
                            <img src="{{LOGO_URL}}" alt="Morii Coffee" width="150">
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px;">
                            <h1 style="color: #333; font-size: 24px; margin: 0 0 20px 0;">Welcome, {{NAME}}!</h1>
                            <p style="color: #666; font-size: 16px; line-height: 1.5;">Your message here.</p>
                        </td>
                    </tr>
                    <!-- CTA Button -->
                    <tr>
                        <td style="padding: 0 30px 40px 30px; text-align: center;">
                            <a href="{{CTA_URL}}" style="background-color: #8B4513; color: #ffffff; padding: 12px 24px; text-decoration: none; display: inline-block; border-radius: 4px; font-weight: bold;">Shop Now</a>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
```

---

## 3. OAuth2 Authorization Code Flow

### Decision: Standard Authorization Code Flow with State Parameter (CSRF Protection)

**Rationale**:
- Authorization Code Flow is the most secure OAuth2 flow for server-side applications
- State parameter prevents CSRF attacks by ensuring the callback originated from the legitimate authorization request
- Google and Meta both support this flow with well-documented APIs
- Backend exchanges authorization code for access token server-to-server (access token never exposed to browser)

**Alternatives Considered**:
- **Implicit Flow**: Deprecated for security reasons; access token exposed in URL fragment (rejected)
- **PKCE Extension**: More secure but adds complexity; not required for server-side apps (deferred to future enhancement)
- **Client Credentials Flow**: For machine-to-machine auth, not suitable for user sign-in (rejected)

**Implementation Notes**:

### Google OAuth2 Setup

**Required Configuration**:
- **Client ID**: Generated from Google Cloud Console
- **Client Secret**: Generated from Google Cloud Console (keep secure)
- **Redirect URI**: `https://moriicoffee.com/auth/callback` (must match exactly)
- **Scopes**: `openid`, `email`, `profile` (request email and profile info)

**Setup Steps**:
1. Create project in [Google Cloud Console](https://console.cloud.google.com/)
2. Enable Google+ API
3. Create OAuth2 credentials (Application type: Web application)
4. Configure authorized redirect URIs: `http://localhost:3000/auth/callback`, `https://moriicoffee.com/auth/callback`
5. Copy Client ID and Client Secret

**Authorization URL Generation**:
```csharp
public string GenerateGoogleAuthUrl(string redirectUri, string state)
{
    var baseUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    var queryParams = new Dictionary<string, string>
    {
        ["client_id"] = _settings.Google.ClientId,
        ["redirect_uri"] = redirectUri,
        ["response_type"] = "code",
        ["scope"] = "openid email profile",
        ["state"] = state,
        ["access_type"] = "offline",  // Get refresh token
        ["prompt"] = "consent"         // Force consent screen (for refresh token)
    };
    return $"{baseUrl}?{string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"))}";
}
```

**Token Exchange**:
```csharp
public async Task<OAuth2TokenResponse> ExchangeGoogleCodeAsync(string code, string redirectUri)
{
    var tokenUrl = "https://oauth2.googleapis.com/token";
    var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["code"] = code,
        ["client_id"] = _settings.Google.ClientId,
        ["client_secret"] = _settings.Google.ClientSecret,
        ["redirect_uri"] = redirectUri,
        ["grant_type"] = "authorization_code"
    });

    var response = await _httpClient.PostAsync(tokenUrl, content);
    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<OAuth2TokenResponse>(json);
}
```

**User Profile Extraction** (from Google ID Token):
- Google returns an `id_token` (JWT) in the token response
- Decode JWT to extract claims: `sub` (user ID), `email`, `email_verified`, `name`
- Validate JWT signature using Google's public keys (`https://www.googleapis.com/oauth2/v3/certs`)

### Meta (Facebook) OAuth2 Setup

**Required Configuration**:
- **App ID**: Generated from Meta for Developers
- **App Secret**: Generated from Meta for Developers (keep secure)
- **Redirect URI**: `https://moriicoffee.com/auth/callback`
- **Scopes**: `email`, `public_profile` (request email and profile info)

**Setup Steps**:
1. Create app in [Meta for Developers](https://developers.facebook.com/)
2. Add Facebook Login product
3. Configure OAuth redirect URIs: `http://localhost:3000/auth/callback`, `https://moriicoffee.com/auth/callback`
4. Copy App ID and App Secret
5. Submit for App Review (email scope requires review for production)

**Authorization URL Generation**:
```csharp
public string GenerateMetaAuthUrl(string redirectUri, string state)
{
    var baseUrl = "https://www.facebook.com/v18.0/dialog/oauth";
    var queryParams = new Dictionary<string, string>
    {
        ["client_id"] = _settings.Meta.AppId,
        ["redirect_uri"] = redirectUri,
        ["response_type"] = "code",
        ["scope"] = "email,public_profile",
        ["state"] = state
    };
    return $"{baseUrl}?{string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"))}";
}
```

**Token Exchange**:
```csharp
public async Task<OAuth2TokenResponse> ExchangeMetaCodeAsync(string code, string redirectUri)
{
    var tokenUrl = "https://graph.facebook.com/v18.0/oauth/access_token";
    var url = $"{tokenUrl}?code={code}&client_id={_settings.Meta.AppId}&client_secret={_settings.Meta.AppSecret}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

    var response = await _httpClient.GetAsync(url);
    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<OAuth2TokenResponse>(json);
}
```

**User Profile Fetch**:
- Meta does not return user info in token response; must call Graph API
- Endpoint: `https://graph.facebook.com/v18.0/me?fields=id,email,name&access_token={access_token}`
- Response includes: `id` (user ID), `email`, `name`

### State Parameter (CSRF Protection)

**Security Requirements**:
- Generate cryptographically secure random state value (32 bytes minimum)
- Store state in distributed cache (Redis) or encrypted cookie with 10-minute expiration
- Validate state parameter in callback; reject if mismatch

**Implementation**:
```csharp
public class OAuth2StateManager
{
    private readonly IDistributedCache _cache;

    public async Task<string> GenerateStateAsync()
    {
        var stateBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(stateBytes);
        }

        var state = Convert.ToBase64String(stateBytes);
        await _cache.SetStringAsync($"oauth2:state:{state}", "valid", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return state;
    }

    public async Task<bool> ValidateStateAsync(string state)
    {
        var cachedState = await _cache.GetStringAsync($"oauth2:state:{state}");
        if (cachedState == null) return false;

        await _cache.RemoveAsync($"oauth2:state:{state}");  // One-time use
        return true;
    }
}
```

---

## 4. Account Linking Strategies

### Decision: Link-on-Login (Email-Based Linking) with Email Verification Requirement

**Rationale**:
- Automatically links social accounts to existing email-based accounts when verified email matches
- Prevents account fragmentation (one user, one account, multiple login methods)
- Requires email verification from OAuth2 provider to prevent pre-account takeover attacks
- Sends notification email when new login method added for security awareness

**Alternatives Considered**:
- **Manual Linking Only**: Requires user to explicitly link accounts in profile settings (rejected: poor UX, causes duplicate accounts)
- **Pre-Account Takeover Prevention (PAT)**: Sends verification email before linking (rejected: adds friction, OAuth email already verified)
- **No Linking (Separate Accounts)**: Each login method creates separate account (rejected: confusing for users)

**Implementation Notes**:

### Link-on-Login Flow

```csharp
public async Task<AuthResponseDto> HandleSocialLoginAsync(string provider, string providerId, string email, bool emailVerified)
{
    // Security: Only link if OAuth provider verified the email
    if (!emailVerified)
        throw new BadRequestException("Email not verified by provider. Please verify your email before using social login.");

    // Check if user exists by email
    var existingUser = await _usersRepository.GetByEmailAsync(email);

    if (existingUser != null)
    {
        // User exists - link external provider if not already linked
        if (existingUser.ExternalProvider == EExternalProvider.None)
        {
            existingUser.LinkExternalProvider(
                provider == "google" ? EExternalProvider.Google : EExternalProvider.Meta,
                providerId,
                email,
                emailVerified
            );
            await _unitOfWork.CommitAsync();

            // Send notification email
            _ = _emailService.SendAccountLinkNotificationAsync(email, provider);

            _logger.LogInformation("Linked {Provider} account to existing user {Email}", provider, email);
        }
        else if (existingUser.ExternalProviderId != providerId)
        {
            // Email conflict: same email, different provider
            throw new ConflictException($"This email is already linked to {existingUser.ExternalProvider}. Please sign in with that provider.");
        }
    }
    else
    {
        // User does not exist - create new account with external provider
        existingUser = new User
        {
            Email = email,
            UserName = email.Split('@')[0],  // Generate username from email
            ExternalProvider = provider == "google" ? EExternalProvider.Google : EExternalProvider.Meta,
            ExternalProviderId = providerId,
            ExternalEmail = email,
            ExternalEmailVerified = emailVerified,
            Status = EUserStatus.Active
        };

        await _usersRepository.AddAsync(existingUser);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Created new user from {Provider} login: {Email}", provider, email);
    }

    // Generate JWT tokens
    var accessToken = await _tokenService.GenerateAccessTokenAsync(existingUser);
    var refreshToken = await _userManager.GenerateUserTokenAsync(existingUser, "DEFAULT", "REFRESH");
    await _userManager.SetAuthenticationTokenAsync(existingUser, "DEFAULT", "REFRESH", refreshToken, CancellationToken.None);

    return new AuthResponseDto { AccessToken = accessToken, RefreshToken = refreshToken };
}
```

### Edge Case Handling

**Email Already Registered with Local Account**:
- **Behavior**: Automatically link external provider to existing account
- **Security**: Only link if `email_verified == true` from OAuth provider
- **Notification**: Send email to user notifying them of new login method

**Email Already Registered with Different Provider**:
- **Behavior**: Reject with 409 Conflict error
- **Error Message**: "This email is already linked to [Google/Meta]. Please sign in with that provider."
- **Rationale**: Prevents account hijacking; forces user to use original login method

**Email Not Verified by OAuth Provider**:
- **Behavior**: Reject with 400 Bad Request
- **Error Message**: "Email not verified by provider. Please verify your email with [Google/Meta] before using social login."
- **Rationale**: Prevents pre-account takeover attacks (attacker creates OAuth account with victim's email before victim registers)

**User Denies OAuth Consent**:
- **Behavior**: OAuth provider redirects with `error=access_denied` query param
- **Frontend Handling**: Check for `error` param; show friendly message and redirect to sign-in page
- **Error Message**: "You cancelled the sign-in process. Please try again or use email/password."

### Database Schema (Planned)

**User Entity Extensions**:
```csharp
public class User : IdentityUser<Guid>
{
    // Existing fields...

    // NEW fields for social login
    public EExternalProvider ExternalProvider { get; set; } = EExternalProvider.None;
    public string? ExternalProviderId { get; set; }  // Max 500 chars
    public string? ExternalEmail { get; set; }       // Max 320 chars
    public bool ExternalEmailVerified { get; set; } = false;
}

public enum EExternalProvider
{
    None = 0,
    Google = 1,
    Meta = 2
}
```

**Migration** (planned):
```sql
ALTER TABLE AspNetUsers ADD ExternalProvider INT NULL DEFAULT 0;
ALTER TABLE AspNetUsers ADD ExternalProviderId NVARCHAR(500) NULL;
ALTER TABLE AspNetUsers ADD ExternalEmail NVARCHAR(320) NULL;
ALTER TABLE AspNetUsers ADD ExternalEmailVerified BIT NOT NULL DEFAULT 0;

CREATE INDEX IX_Users_ExternalProvider_ExternalProviderId
ON AspNetUsers (ExternalProvider, ExternalProviderId);
```

---

## Summary of Decisions

| Topic | Decision | Key Rationale |
|-------|----------|---------------|
| **Email Service** | SendGrid with fire-and-forget pattern | 99.99% uptime SLA; graceful degradation on failure |
| **Email Templates** | Table-based layout with inline CSS (hex colors) | Gmail compatibility; Outlook compatibility |
| **OAuth2 Flow** | Authorization Code Flow with state parameter | Most secure for server-side apps; CSRF protection |
| **Google OAuth** | Scopes: openid, email, profile | Standard claims; email verification included |
| **Meta OAuth** | Scopes: email, public_profile | Basic profile + email; requires App Review |
| **Account Linking** | Automatic link-on-login (email match) | Prevents duplicate accounts; requires email_verified |
| **Email Conflict** | Reject with 409 Conflict error | Prevents account hijacking |
| **Unverified Email** | Reject with 400 Bad Request | Prevents pre-account takeover |

---

## Next Steps (Phase 1)

1. Create HTML email templates (`welcome.html`, `password-reset.html`) with Morii Coffee branding
2. Update `EmailTemplates.cs` helper to load and parse templates
3. Update `SendGridEmailService.cs` to implement `SendWelcomeEmailAsync()` and `SendPasswordResetEmailAsync()`
4. Update `EmailSettings.cs` to add `ResetPasswordBaseUrl` property
5. Update command handlers (`SignUpCommandHandler`, `ForgotPasswordCommandHandler`) to call email service
6. Document data model extensions for social login (User entity changes)
7. Document API endpoint contracts for social login
8. Generate quickstart guide for email testing and OAuth2 setup

---

## References

- [SendGrid .NET Documentation](https://docs.sendgrid.com/for-developers/sending-email/v3-csharp-code-example)
- [Gmail CSS Support](https://developers.google.com/gmail/design/css)
- [Outlook Email Rendering](https://www.hteumeuleu.com/2021/a-guide-to-outlook/)
- [Google OAuth2 Setup](https://developers.google.com/identity/protocols/oauth2/web-server)
- [Meta OAuth2 Documentation](https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow)
- [OAuth2 Security Best Practices](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
- [Email Template Best Practices](https://reallygoodemails.com/resources/email-design-best-practices)

---

**Full Research Document**: `/docs/oauth-email-best-practices.md` (comprehensive technical details and code examples)
