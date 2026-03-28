# Google OAuth Integration Guide for Next.js Frontend

**Feature**: Google OAuth External Authentication  
**Backend Branch**: `005-google-oauth`  
**Last Updated**: 2026-03-28  
**Target**: Next.js Frontend (morii-coffee-fe)

---

## Overview

This guide explains how to integrate Google OAuth authentication with the MoriiCoffee Next.js frontend. The backend provides two endpoints for OAuth flow:

1. **POST /api/v1/auth/external-login** - Initiates OAuth flow
2. **GET /api/v1/auth/external-auth-callback** - Processes callback and issues tokens

---

## OAuth Flow Diagram

```
User clicks "Sign in with Google"
    ↓
Frontend redirects to: POST /api/v1/auth/external-login?provider=Google&returnUrl=/dashboard
    ↓
Backend redirects to Google OAuth consent screen
    ↓
User authenticates and grants permissions on Google
    ↓
Google redirects to: GET /api/v1/auth/external-auth-callback?code=...&state=...&returnUrl=/dashboard
    ↓
Backend processes callback:
  - Creates account if new user
  - Assigns CUSTOMER role
  - Sends welcome email
  - Generates JWT tokens
    ↓
Backend sets AuthTokenHolder cookie and redirects to returnUrl
    ↓
Frontend extracts tokens from cookie and stores them
    ↓
User is authenticated and redirected to dashboard
```

---

## Backend Endpoints

### 1. Initiate OAuth Flow

**Endpoint**: `POST /api/v1/auth/external-login`

**Query Parameters**:
- `provider` (string, required): OAuth provider name. Must be "Google" (case-insensitive).
- `returnUrl` (string, optional): URL to redirect after authentication. Defaults to "/".

**Response**: HTTP 302 redirect to Google's OAuth consent screen.

**Example**:
```
POST http://localhost:8002/api/v1/auth/external-login?provider=Google&returnUrl=/dashboard
```

---

### 2. OAuth Callback (Handled by Backend)

**Endpoint**: `GET /api/v1/auth/external-auth-callback`

**Query Parameters**:
- `code` (string): Authorization code from Google
- `state` (string): CSRF protection token
- `returnUrl` (string): URL to redirect after processing
- `error` (string, optional): Error code if user denied
- `error_description` (string, optional): Error description

**Response**: HTTP 302 redirect to `returnUrl` with `AuthTokenHolder` cookie containing tokens.

**Cookie Format**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4e5f6..."
}
```

**Cookie Properties**:
- Name: `AuthTokenHolder`
- Max-Age: 300 seconds (5 minutes)
- HttpOnly: true
- Secure: true (HTTPS only in production)
- SameSite: Strict
- Path: /

---

## Next.js Frontend Implementation

### Step 1: Create OAuth Button Component

```tsx
// components/auth/GoogleSignInButton.tsx
'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';

export default function GoogleSignInButton() {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);

  const handleGoogleSignIn = () => {
    setIsLoading(true);
    
    // Get current path to return to after authentication
    const returnUrl = window.location.pathname;
    
    // Construct backend OAuth initiation URL
    const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8002';
    const oauthUrl = `${apiBaseUrl}/api/v1/auth/external-login?provider=Google&returnUrl=${encodeURIComponent(returnUrl)}`;
    
    // Redirect to backend OAuth endpoint
    // Backend will redirect to Google, then back to callback
    window.location.href = oauthUrl;
  };

  return (
    <button
      onClick={handleGoogleSignIn}
      disabled={isLoading}
      className="flex items-center justify-center gap-2 w-full px-4 py-2 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
    >
      {isLoading ? (
        <span>Redirecting...</span>
      ) : (
        <>
          <svg className="w-5 h-5" viewBox="0 0 24 24">
            {/* Google logo SVG */}
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
          </svg>
          <span>Sign in with Google</span>
        </>
      )}
    </button>
  );
}
```

---

### Step 2: Create Callback Page

Create a page to handle the OAuth callback and extract tokens:

```tsx
// app/auth/callback/page.tsx
'use client';

import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

export default function AuthCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Extract tokens from cookie
    const extractTokens = () => {
      // Get AuthTokenHolder cookie
      const cookies = document.cookie.split('; ');
      const authCookie = cookies.find(cookie => cookie.startsWith('AuthTokenHolder='));
      
      if (!authCookie) {
        // Check for error in URL
        const errorParam = searchParams.get('error');
        if (errorParam) {
          const errorMsg = searchParams.get('message') || 'Authentication failed';
          setError(decodeURIComponent(errorMsg));
          return;
        }
        
        setError('No authentication token received. Please try again.');
        return;
      }

      try {
        // Parse cookie value
        const cookieValue = authCookie.split('=')[1];
        const decodedValue = decodeURIComponent(cookieValue);
        const tokens = JSON.parse(decodedValue);

        // Store tokens in localStorage or your preferred storage
        localStorage.setItem('accessToken', tokens.accessToken);
        localStorage.setItem('refreshToken', tokens.refreshToken);

        // Delete the cookie after extraction
        document.cookie = 'AuthTokenHolder=; Max-Age=0; path=/;';

        // Fetch user profile
        fetchUserProfile(tokens.accessToken);
      } catch (err) {
        console.error('Failed to extract tokens:', err);
        setError('Failed to process authentication. Please try again.');
      }
    };

    extractTokens();
  }, [searchParams]);

  const fetchUserProfile = async (accessToken: string) => {
    try {
      const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8002';
      const response = await fetch(`${apiBaseUrl}/api/v1/users/me`, {
        headers: {
          'Authorization': `Bearer ${accessToken}`,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to fetch user profile');
      }

      const user = await response.json();
      
      // Store user profile in your state management (Redux, Zustand, etc.)
      localStorage.setItem('user', JSON.stringify(user));

      // Redirect to intended page or dashboard
      const returnUrl = searchParams.get('returnUrl') || '/dashboard';
      router.push(returnUrl);
    } catch (err) {
      console.error('Failed to fetch user profile:', err);
      setError('Failed to load user profile. Please try signing in again.');
    }
  };

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center min-h-screen p-4">
        <div className="max-w-md w-full bg-red-50 border border-red-200 rounded-lg p-6">
          <h2 className="text-xl font-semibold text-red-800 mb-2">
            Authentication Error
          </h2>
          <p className="text-red-600 mb-4">{error}</p>
          <button
            onClick={() => router.push('/sign-in')}
            className="w-full px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
          >
            Back to Sign In
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-gray-900 mx-auto mb-4"></div>
        <p className="text-gray-600">Completing sign in...</p>
      </div>
    </div>
  );
}
```

---

### Step 3: Create Authentication Context/Hook

```tsx
// lib/auth/useAuth.ts
'use client';

import { useState, useEffect, createContext, useContext, ReactNode } from 'react';
import { useRouter } from 'next/navigation';

interface User {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  signOut: () => void;
  refreshToken: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    // Load user from localStorage on mount
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      setUser(JSON.parse(storedUser));
    }
    setIsLoading(false);
  }, []);

  const signOut = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    setUser(null);
    router.push('/sign-in');
  };

  const refreshToken = async (): Promise<boolean> => {
    try {
      const currentRefreshToken = localStorage.getItem('refreshToken');
      const currentAccessToken = localStorage.getItem('accessToken');
      
      if (!currentRefreshToken || !currentAccessToken) {
        return false;
      }

      const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8002';
      const response = await fetch(`${apiBaseUrl}/api/v1/auth/refresh-token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${currentAccessToken}`,
        },
        body: JSON.stringify({
          refreshToken: currentRefreshToken,
        }),
      });

      if (!response.ok) {
        return false;
      }

      const data = await response.json();
      localStorage.setItem('accessToken', data.accessToken);
      localStorage.setItem('refreshToken', data.refreshToken);
      localStorage.setItem('user', JSON.stringify(data.user));
      setUser(data.user);

      return true;
    } catch (err) {
      console.error('Token refresh failed:', err);
      return false;
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        signOut,
        refreshToken,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
```

---

### Step 4: Create API Client with Token Refresh

```tsx
// lib/api/apiClient.ts
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8002';

export class ApiClient {
  private static async getAccessToken(): Promise<string | null> {
    return localStorage.getItem('accessToken');
  }

  private static async refreshAccessToken(): Promise<boolean> {
    try {
      const refreshToken = localStorage.getItem('refreshToken');
      const accessToken = localStorage.getItem('accessToken');
      
      if (!refreshToken || !accessToken) {
        return false;
      }

      const response = await fetch(`${API_BASE_URL}/api/v1/auth/refresh-token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ refreshToken }),
      });

      if (!response.ok) {
        return false;
      }

      const data = await response.json();
      localStorage.setItem('accessToken', data.accessToken);
      localStorage.setItem('refreshToken', data.refreshToken);
      
      return true;
    } catch (err) {
      console.error('Token refresh failed:', err);
      return false;
    }
  }

  static async fetch(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<Response> {
    const accessToken = await this.getAccessToken();

    const headers = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    if (accessToken) {
      headers['Authorization'] = `Bearer ${accessToken}`;
    }

    let response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers,
    });

    // If 401, try refreshing token once
    if (response.status === 401) {
      const refreshed = await this.refreshAccessToken();
      
      if (refreshed) {
        // Retry request with new token
        const newAccessToken = await this.getAccessToken();
        if (newAccessToken) {
          headers['Authorization'] = `Bearer ${newAccessToken}`;
        }
        
        response = await fetch(`${API_BASE_URL}${endpoint}`, {
          ...options,
          headers,
        });
      } else {
        // Refresh failed, redirect to sign-in
        localStorage.clear();
        window.location.href = '/sign-in';
      }
    }

    return response;
  }
}
```

---

### Step 5: Update Sign-In Page

```tsx
// app/sign-in/page.tsx
import GoogleSignInButton from '@/components/auth/GoogleSignInButton';

export default function SignInPage() {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen p-4">
      <div className="max-w-md w-full space-y-8">
        <div className="text-center">
          <h2 className="text-3xl font-bold">Sign in to MoriiCoffee</h2>
          <p className="mt-2 text-gray-600">
            Sign in with your Google account or email
          </p>
        </div>

        <div className="space-y-4">
          {/* Google OAuth Button */}
          <GoogleSignInButton />

          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-300" />
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="px-2 bg-white text-gray-500">
                Or continue with email
              </span>
            </div>
          </div>

          {/* Email/Password Form */}
          <form className="space-y-4">
            {/* Your existing email/password form */}
          </form>
        </div>
      </div>
    </div>
  );
}
```

---

## Environment Variables

Add to your `.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:8002
```

For production:

```env
NEXT_PUBLIC_API_URL=https://api.moriicoffee.com
```

---

## Testing the Integration

### 1. Local Development

1. **Start Backend**:
   ```bash
   cd morii-coffee
   bash deploy/run-docker-development.sh
   ```

2. **Start Frontend**:
   ```bash
   cd morii-coffee-fe
   npm run dev
   ```

3. **Test OAuth Flow**:
   - Navigate to http://localhost:3000/sign-in
   - Click "Sign in with Google"
   - Complete Google authentication
   - Verify redirect back to your app with tokens

---

### 2. Verify Token Storage

Open browser console after successful sign-in:

```javascript
// Check tokens are stored
console.log('Access Token:', localStorage.getItem('accessToken'));
console.log('Refresh Token:', localStorage.getItem('refreshToken'));
console.log('User:', JSON.parse(localStorage.getItem('user')));
```

---

### 3. Test Authenticated API Calls

```javascript
// Example: Fetch user profile
import { ApiClient } from '@/lib/api/apiClient';

const response = await ApiClient.fetch('/api/v1/users/me');
const user = await response.json();
console.log('Current user:', user);
```

---

## Error Handling

### Common Errors and Solutions

#### 1. "No authentication token received"

**Cause**: Cookie not set or expired before extraction.

**Solution**:
- Ensure callback page extracts tokens immediately
- Check cookie settings (HttpOnly must be false if extracting from JS, or use a different approach)
- Verify backend sets cookie correctly

#### 2. "CORS Error"

**Cause**: Frontend and backend on different origins without CORS configuration.

**Solution**:
- Backend must allow frontend origin in CORS settings
- Check `source/MoriiCoffee.Presentation/appsettings.json` CORS configuration

#### 3. "Invalid state parameter"

**Cause**: CSRF token validation failed.

**Solution**:
- Complete OAuth flow within 15 minutes
- Don't clear cookies between initiation and callback
- Restart OAuth flow

#### 4. "User denied permission"

**Cause**: User clicked "Cancel" on Google consent screen.

**Solution**:
- Display friendly error message
- Offer to retry or use email/password sign-in

---

## Security Considerations

### 1. Token Storage

**Current Approach**: localStorage (simple, works for most cases)

**Production Recommendations**:
- Consider using httpOnly cookies for tokens (requires backend changes)
- Implement token rotation on every request
- Clear tokens on sign out

### 2. HTTPS Required

**Development**: HTTP is acceptable for localhost

**Production**: MUST use HTTPS for:
- OAuth redirect URIs
- Cookie security (Secure flag)
- Token transmission

### 3. State Management

**Recommendations**:
- Store user profile in React Context or state management library (Redux, Zustand)
- Don't rely solely on localStorage for auth state
- Implement proper loading and error states

---

## Production Deployment

### 1. Update Google OAuth Credentials

In Google Cloud Console:
- Add production redirect URI: `https://api.moriicoffee.com/api/v1/auth/external-auth-callback`
- Add authorized origin: `https://moriicoffee.com`

### 2. Update Backend Configuration

In `appsettings.Production.json`:
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_PRODUCTION_CLIENT_ID",
      "ClientSecret": "YOUR_PRODUCTION_CLIENT_SECRET"
    }
  }
}
```

### 3. Update Frontend Environment Variables

```env
NEXT_PUBLIC_API_URL=https://api.moriicoffee.com
```

---

## Troubleshooting

### Check Backend Logs

```bash
docker logs morii-coffee-api | grep -i "google\|oauth"
```

### Verify OAuth Configuration

```bash
# Check if Google OAuth is configured
curl http://localhost:8002/api/v1/auth/external-login?provider=Google
# Should return 302 redirect to Google
```

### Test Token Refresh

```javascript
const response = await fetch('http://localhost:8002/api/v1/auth/refresh-token', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${accessToken}`,
  },
  body: JSON.stringify({
    refreshToken: refreshToken,
  }),
});

const data = await response.json();
console.log('New tokens:', data);
```

---

## Next Steps

1. **Implement in your Next.js app**:
   - Add GoogleSignInButton component
   - Create auth callback page
   - Set up AuthProvider context

2. **Test locally**:
   - Complete OAuth flow
   - Verify tokens stored correctly
   - Test authenticated API calls

3. **Handle edge cases**:
   - Token expiration
   - Refresh token flow
   - Sign out functionality

4. **Prepare for production**:
   - Update Google OAuth credentials with production URLs
   - Set up environment variables
   - Test on staging environment

---

## Support

For issues or questions:
1. Check backend logs: `docker logs morii-coffee-api`
2. Verify Google Cloud Console settings
3. Review this integration guide
4. Check backend documentation: `/docs/features/google-auth/google-auth-explaination.md`

---

**Last Updated**: 2026-03-28  
**Backend Version**: 005-google-oauth  
**Compatible Frontend**: Next.js 14+
