# Email Integration Guide for Frontend

**Purpose**: Documentation for frontend developers to understand email content and integration points for Morii Coffee transactional emails.

**Last Updated**: 2026-03-27

---

## Overview

Morii Coffee sends two types of transactional emails via Brevo (Sendinblue):

1. **Welcome Email** - Sent immediately after successful user registration
2. **Password Reset Email** - Sent when user requests to reset their password

Both emails are sent automatically by the backend and do not require frontend action to trigger.

---

## 1. Welcome Email

### When It's Sent

Automatically sent after **successful account creation** via `POST /api/v1/auth/signup`.

**Trigger Flow**:
```
User submits signup form
  ↓
POST /api/v1/auth/signup (Backend)
  ↓
Account created successfully
  ↓
Welcome email sent (async, fire-and-forget)
  ↓
Frontend receives signup response (does NOT wait for email)
```

### Email Content

**Subject**: `Welcome to Morii Coffee, {UserName}!`

**From**: `Morii Coffee <huulongn15@gmail.com>` (or configured sender)

**Email Body** (HTML):

```
┌─────────────────────────────────────────┐
│     Welcome to Morii Coffee!            │  ← Header (brown background)
└─────────────────────────────────────────┘

Hi {UserName},

Thank you for joining Morii Coffee! We're excited to
have you as part of our community.

Your account has been successfully created and you can now:
• Browse our premium coffee selection
• Place orders online
• Track your order history
• Manage your account preferences

        ┌──────────────────┐
        │  Start Shopping  │  ← Button (links to storefront)
        └──────────────────┘

If you have any questions, feel free to reach out to
our support team.

Cheers,
The Morii Coffee Team

© 2026 Morii Coffee. All rights reserved.
```

### Personalization

**Replaced Placeholders**:
- `{{UserName}}` → User's username (from signup request)
- `{{StorefrontUrl}}` → Frontend base URL (default: `http://localhost:3000`)

**Button Link**:
- Text: "Start Shopping"
- URL: `{StorefrontUrl}` (e.g., `http://localhost:3000`)
- Opens homepage/product listing in browser

### Frontend Requirements

**No frontend action required**. The email is sent automatically by the backend.

**User Experience Tips**:
- Show a success message after signup: "Account created! Check your email for a welcome message."
- Optionally redirect to dashboard or products page
- Email delivery is fire-and-forget (signup succeeds even if email fails)

---

## 2. Password Reset Email

### When It's Sent

Sent when user requests password reset via `POST /api/v1/auth/forgot-password`.

**Trigger Flow**:
```
User clicks "Forgot Password"
  ↓
POST /api/v1/auth/forgot-password (Backend)
  ↓
User lookup by email
  ↓
If user exists: Password reset email sent (async)
If user not exists: No email sent, but returns same success message
  ↓
Frontend receives success response (always 200)
```

**⚠️ Important**: Backend always returns success to prevent email enumeration attacks. Frontend should show the same message regardless of whether the email exists.

### Email Content

**Subject**: `Reset Your Morii Coffee Password`

**From**: `Morii Coffee <huulongn15@gmail.com>` (or configured sender)

**Email Body** (HTML):

```
┌─────────────────────────────────────────┐
│     Password Reset Request              │  ← Header (red background)
└─────────────────────────────────────────┘

Hi {UserName},

We received a request to reset your Morii Coffee
account password.

Click the button below to create a new password:

        ┌──────────────────┐
        │  Reset Password  │  ← Button (links to reset page)
        └──────────────────┘

⚠️ Security Notice:
This link will expire soon and can only be used once.
If you didn't request this password reset, please
ignore this email and your password will remain
unchanged.

If the button doesn't work, you can copy and paste
this link into your browser:

{ResetUrl}

Best regards,
The Morii Coffee Team

© 2026 Morii Coffee. All rights reserved.
If you need assistance, please contact our support team.
```

### Personalization

**Replaced Placeholders**:
- `{{UserName}}` → User's display name (FullName → UserName → "there" as fallback)
- `{{ResetUrl}}` → Complete reset URL with encoded token and email

### Reset URL Format

**Complete URL Structure**:
```
{ResetPasswordBaseUrl}?token={encodedToken}&email={encodedEmail}
```

**Example**:
```
http://localhost:3000/reset-password?token=Q3pSR1RubG1NVzlPZV...&email=c2F5aGVsbG8yMDAyMUBnbWFpbC5jb20%3D
```

**URL Parameters**:

| Parameter | Description | Encoding |
|-----------|-------------|----------|
| `token` | Password reset token | Base64URL encoded (ASP.NET Identity token) |
| `email` | User's email address | Base64URL encoded |

**Encoding Details**:
- **Base64URL Encoding**: Uses URL-safe characters (no `+`, `/`, or `=` padding issues)
- **Format**: `WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(value))`
- **Length**: Varies (tokens are ~100-200 characters when encoded)

### Frontend Requirements

#### 1. Reset Password Page Route

Create a page at the configured `ResetPasswordBaseUrl` (default: `/reset-password`).

**Route**: `/reset-password`

**Query Parameters**:
- `token` (required): Base64URL-encoded reset token
- `email` (required): Base64URL-encoded email address

#### 2. Frontend Implementation

**Step 1: Extract URL Parameters**

```typescript
// Example: Next.js or React
import { useSearchParams } from 'next/navigation';

function ResetPasswordPage() {
  const searchParams = useSearchParams();
  const encodedToken = searchParams.get('token');
  const encodedEmail = searchParams.get('email');

  if (!encodedToken || !encodedEmail) {
    return <ErrorMessage>Invalid or missing reset link</ErrorMessage>;
  }

  // Continue with form...
}
```

**Step 2: Decode Parameters (Optional)**

```typescript
// Decode Base64URL to display email (optional - for UX)
function base64UrlDecode(str: string): string {
  // Convert Base64URL to standard Base64
  let base64 = str.replace(/-/g, '+').replace(/_/g, '/');
  // Add padding if needed
  while (base64.length % 4) {
    base64 += '=';
  }
  // Decode
  return atob(base64);
}

const decodedEmail = base64UrlDecode(encodedEmail);
// Display: "Resetting password for user@example.com"
```

**⚠️ Important**: Send the **encoded** values to the backend, not decoded!

**Step 3: Reset Password Form**

```tsx
function ResetPasswordPage() {
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (newPassword !== confirmPassword) {
      alert('Passwords do not match');
      return;
    }

    try {
      const response = await fetch('/api/v1/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: encodedEmail,      // Send encoded value!
          token: encodedToken,       // Send encoded value!
          newPassword: newPassword
        })
      });

      if (response.ok) {
        // Success! Redirect to login
        router.push('/login?message=Password reset successful');
      } else {
        // Handle error (expired token, invalid token, etc.)
        const error = await response.json();
        alert(error.message);
      }
    } catch (error) {
      console.error('Reset failed:', error);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="password"
        placeholder="New Password"
        value={newPassword}
        onChange={(e) => setNewPassword(e.target.value)}
        required
      />
      <input
        type="password"
        placeholder="Confirm Password"
        value={confirmPassword}
        onChange={(e) => setConfirmPassword(e.target.value)}
        required
      />
      <button type="submit">Reset Password</button>
    </form>
  );
}
```

**Step 4: Send to Backend API**

**Endpoint**: `POST /api/v1/auth/reset-password`

**Request Body**:
```json
{
  "email": "c2F5aGVsbG8yMDAyMUBnbWFpbC5jb20=",
  "token": "Q3pSR1RubG1NVzlPZVVkU2FGSmxOMFZ0TVRaMk5ISnRTSGhKVVRCdldWUkpNRTFVVlhsT2VtczFUa1JSTWsxRVJUVk9ha1Y2VFZScmVrMTZXWGRQVkdkNVRWUkplVTVVVVhsT1ZGRXlUVmROZDA1SFVUVk5WR3Q2VFZSVmVscEVVWGxhVkVVMFQwZFpORTFVV1ROTmFrVjZUbnBOZUUxcVNUQk5hbWN4VGpKTmVrMTZRVEJOUkZGNlRsZEtiRTlVVVhwTlZFazBXbXBSTTAxWFJUTlBSRkV6VG1wRmVFOUhTVEpOUkVVeVdXMVJlRTFxWnpSUFYwazFXbXBWTWsxcVJYZE5SR042VGpKRk0wMXRVbXBOYW14M1RsUnJNMDlVVm0xUFJHUnBUVlJyZDA5SFVYbE9SRkV6VDFSb2FVOVVUWGhOYWtWNVRXcFpNVTVVVG0xT1JHaHBUMGRaZUUxRVFYbE5WMDAwVDFSUmVrOUhhR2hPUkdNMFdtcEthazlFYkdwTlZFcHNXVlJWZWs1SFJtcE5hazB5VG5wTmVVOUVWbWhPYW1NeVRucFJNazFFVFhwT2VrVXdUakpHYWs1VVRYcGFWRWt4V21wQ2JVOVVWVEpOVkVWNVRucE5NMDlFVFRKTlJGVjVUbTFOTWsxWFJtMVplbU14VGtSQmVVOVVRWGxOVkUweVdWZE5NMDFVVm1oT1IxazBUMVJLYVU1RVJtcE9hbFY2VGxSQmQwNUVWWGxaYWswMFRsUkJlVTlFUVROTlJFazBUakpPYVU1RVRYbE5hbHBwV1ZSRk1VNVVUbWhOYWxWNVRqSlplRTFVYTNsWmJVWnRXa1JKTlU1SFdUQlBWMVY1VGtSck5FOUVZelZPYWxreFRucE9hVnBFWXpsT2Fra3hXa2RSZWs1VVRUVlBWRVY2VDBSQk0wOVVRbWhhUkVFd1dWUlJNazlFWjNsWmJWSnBXV3BqZWsxVVRURk9hbGw2VFZkUk1FMUVZekJQVkZFelQxZE5NMDlIV1hoUFZGVTFXVEpKZUU1RVozZE5hbWN4VGtkT2FGbFVaekJQVkd0NFRqSkZORTVFV21wT01sazBUakpGZWxwcVNUVlBWRlY2VFZSRk0wNUVRbXBhUjBsNlRsUm5NMDFVVFRGUFZGRTFXV3BuTTA1RVNtbE9WMWw1VGtSbk0wNVhWbXBOVkVreFQwUm5lRmxxVFhsT2Fsazk=",
  "newPassword": "NewPassword@123"
}
```

**Success Response** (200):
```json
{
  "statusCode": 200,
  "message": "Success",
  "data": "Password reset successfully.",
  "errors": null
}
```

**Error Responses**:

| Status | Message | Cause |
|--------|---------|-------|
| 400 | "Invalid or expired reset token" | Token expired or already used |
| 400 | "Invalid email or token format" | Malformed parameters |
| 400 | "Password does not meet requirements" | Weak password |

---

## Frontend User Experience Recommendations

### Forgot Password Flow

1. **Request Page** (`/forgot-password`):
   ```
   Enter your email address:
   [email input]
   [Send Reset Link]
   ```

2. **After Submit**:
   ```
   ✓ If an account with that email exists,
     we've sent a password reset link.
     Please check your inbox.
   ```

   **⚠️ Important**: Always show the same message (don't reveal if email exists)

3. **Check Email Instruction**:
   ```
   Didn't receive the email?
   • Check your spam folder
   • Make sure you entered the correct email
   • Wait a few minutes and try again
   ```

### Reset Password Page

1. **Before Form** (if token/email missing):
   ```
   ❌ Invalid or missing reset link
   Please request a new password reset.
   [Back to Forgot Password]
   ```

2. **Form Display**:
   ```
   Reset Password for {decodedEmail}

   New Password: [password input]
   Confirm Password: [password input]

   Password requirements:
   • At least 8 characters
   • Contains uppercase and lowercase
   • Contains numbers
   • Contains special characters

   [Reset Password Button]
   ```

3. **After Success**:
   ```
   ✓ Password reset successful!
   You can now sign in with your new password.

   [Go to Sign In] (auto-redirect in 3 seconds)
   ```

4. **After Failure** (expired token):
   ```
   ❌ This reset link has expired or been used
   Please request a new password reset.

   [Back to Forgot Password]
   ```

---

## Email Delivery Timeline

### Welcome Email
- **Expected Delivery**: Within 1 minute of signup
- **Backend Behavior**: Fire-and-forget (signup succeeds even if email fails)
- **User Impact**: None - account is created regardless

### Password Reset Email
- **Expected Delivery**: Within 1 minute of request
- **Backend Behavior**: Fire-and-forget (request succeeds even if email fails)
- **User Impact**: None - same success message shown regardless

**Note**: Email delivery is handled asynchronously and does not block the API response.

---

## Configuration Values (Backend)

These values are configured in the backend `appsettings.json`:

```json
{
  "EmailSettings": {
    "FromEmail": "huulongn15@gmail.com",
    "FromName": "Morii Coffee",
    "StorefrontUrl": "http://localhost:3000",
    "ResetPasswordBaseUrl": "http://localhost:3000/reset-password"
  }
}
```

**Frontend must match**:
- Reset password page route: `/reset-password` (or update backend config)
- Base URL for testing: `http://localhost:3000`
- Production URLs: Update in backend environment variables

---

## Testing Checklist for Frontend

### Welcome Email Testing
- [ ] User receives email within 1 minute of signup
- [ ] Email contains correct username
- [ ] "Start Shopping" button links to homepage
- [ ] Email displays correctly on mobile and desktop
- [ ] Signup succeeds even if email fails to send

### Password Reset Testing
- [ ] User receives email within 1 minute of forgot password request
- [ ] Email contains correct user name
- [ ] Reset link navigates to `/reset-password` page
- [ ] Token and email parameters are present in URL
- [ ] Reset password form accepts new password
- [ ] Success: Redirects to login page
- [ ] Error: Shows appropriate message for expired/invalid token
- [ ] Link works only once (single-use)

---

## Troubleshooting

### User Didn't Receive Email

**Possible Causes**:
1. Email in spam/junk folder
2. Invalid email address entered
3. Email provider blocking Brevo sender
4. User account doesn't exist (password reset only)

**Frontend Should**:
- Suggest checking spam folder
- Provide "Resend email" option (call same API again)
- Offer support contact if persistent

### Reset Link Doesn't Work

**Possible Causes**:
1. Token expired (default: 24 hours)
2. Token already used (single-use)
3. Email/token parameters missing from URL
4. URL encoding issues (% characters)

**Frontend Should**:
- Validate token/email parameters exist before showing form
- Show clear error message: "This link has expired or been used"
- Provide link back to forgot password page

---

## Contact for Backend Issues

If emails are not being sent or contain incorrect content, check with backend team:
- Email service configuration in `appsettings.json`
- Brevo API key validity
- Sender email verification status
- Application logs for email sending errors

---

## Appendix: Full Example URLs

### Welcome Email Link
```
http://localhost:3000
```

### Password Reset Link Example
```
http://localhost:3000/reset-password?token=Q3pSR1RubG1NVzlPZVVkU2FGSmxOMFZ0TVRaMk5ISnRTSGhKVVRCdldWUkpNRTFVVlhsT2VtczFUa1JSTWsxRVJUVk9ha1Y2VFZScmVrMTZXWGRQVkdkNVRWUkplVTVVVVhsT1ZGRXlUVmROZDA1SFVUVk5WR3Q2VFZSVmVscEVVWGxhVkVVMFQwZFpORTFVV1ROTmFrVjZUbnBOZUUxcVNUQk5hbWN4VGpKTmVrMTZRVEJOUkZGNlRsZEtiRTlVVVhwTlZFazBXbXBSTTAxWFJUTlBSRkV6VG1wRmVFOUhTVEpOUkVVeVdXMVJlRTFxWnpSUFYwazFXbXBWTWsxcVJYZE5SR042VGpKRk0wMXRVbXBOYW14M1RsUnJNMDlVVm0xUFJHUnBUVlJyZDA5SFVYbE9SRkV6VDFSb2FVOVVUWGhOYWtWNVRXcFpNVTVVVG0xT1JHaHBUMGRaZUUxRVFYbE5WMDAwVDFSUmVrOUhhR2hPUkdNMFdtcEthazlFYkdwTlZFcHNXVlJWZWs1SFJtcE5hazB5VG5wTmVVOUVWbWhPYW1NeVRucFJNazFFVFhwT2VrVXdUakpHYWs1VVRYcGFWRWt4V21wQ2JVOVVWVEpOVkVWNVRucE5NMDlFVFRKTlJGVjVUbTFOTWsxWFJtMVplbU14VGtSQmVVOVVRWGxOVkUweVdWZE5NMDFVVm1oT1IxazBUMVJLYVU1RVJtcE9hbFY2VGxSQmQwNUVWWGxaYWswMFRsUkJlVTlFUVROTlJFazBUakpPYVU1RVRYbE5hbHBwV1ZSRk1VNVVUbWhOYWxWNVRqSlplRTFVYTNsWmJVWnRXa1JKTlU1SFdUQlBWMVY1VGtSck5FOUVZelZPYWxreFRucE9hVnBFWXpsT2Fra3hXa2RSZWs1VVRUVlBWRVY2VDBSQk0wOVVRbWhhUkVFd1dWUlJNazlFWjNsWmJWSnBXV3BqZWsxVVRURk9hbGw2VFZkUk1FMUVZekJQVkZFelQxZE5NMDlIV1hoUFZGVTFXVEpKZUU1RVozZE5hbWN4VGtkT2FGbFVaekJQVkd0NFRqSkZORTVFV21wT01sazBUakpGZWxwcVNUVlBWRlY2VFZSRk0wNUVRbXBhUjBsNlRsUm5NMDFVVFRGUFZGRTFXV3BuTTA1RVNtbE9WMWw1VGtSbk0wNVhWbXBOVkVreFQwUm5lRmxxVFhsT2Fsazk%3D&email=c2F5aGVsbG8yMDAyMUBnbWFpbC5jb20%3D
```

**Decoded values** (for reference only - don't send decoded to backend):
- Token: (ASP.NET Identity generated token ~200+ chars)
- Email: `sayhello20021@gmail.com`

---

**End of Email Integration Guide**
