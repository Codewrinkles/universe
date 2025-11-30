# OAuth Setup Guide

## Overview

Codewrinkles supports OAuth 2.0 authentication with Google and GitHub. This guide walks you through the complete setup process for both providers.

## Google OAuth Setup

### 1. Create Google Cloud Project

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Click "Select a project" → "New Project"
3. Enter project name: "Codewrinkles"
4. Click "Create"

### 2. Enable Google+ API

1. In the left sidebar, go to **APIs & Services** → **Library**
2. Search for "Google+ API"
3. Click on it and press **Enable**

### 3. Create OAuth 2.0 Credentials

1. Go to **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **OAuth client ID**
3. If prompted, configure the OAuth consent screen:
   - **User Type**: External
   - **App name**: Codewrinkles
   - **User support email**: your-email@example.com
   - **Developer contact**: your-email@example.com
   - Click **Save and Continue**
   - Skip scopes (default `openid`, `email`, `profile` are sufficient)
   - Click **Save and Continue**
4. Back on Create OAuth client ID screen:
   - **Application type**: Web application
   - **Name**: Codewrinkles Web Client
   - **Authorized redirect URIs**:
     - `https://localhost:7280/api/identity/oauth/google/callback` (Development)
     - `https://api.codewrinkles.com/api/identity/oauth/google/callback` (Production)
   - Click **Create**

### 4. Store Credentials

You'll receive:
- **Client ID**: `xxx.apps.googleusercontent.com`
- **Client Secret**: `GOCSPX-xxx`

Store these in .NET user secrets (see below).

---

## GitHub OAuth Setup

### 1. Create GitHub OAuth App

1. Go to [GitHub Settings → Developer Settings](https://github.com/settings/developers)
2. Click **OAuth Apps** → **New OAuth App**

### 2. Configure OAuth App

Fill in the following:

- **Application name**: Codewrinkles
- **Homepage URL** (Development): `http://localhost:5173`
- **Homepage URL** (Production): `https://codewrinkles.com`
- **Authorization callback URL** (Development): `https://localhost:7280/api/identity/oauth/github/callback`
- **Authorization callback URL** (Production): `https://api.codewrinkles.com/api/identity/oauth/github/callback`

Click **Register application**.

### 3. Generate Client Secret

1. After registration, you'll see your **Client ID**
2. Click **Generate a new client secret**
3. Copy the secret immediately (it won't be shown again)

### 4. Store Credentials

You'll have:
- **Client ID**: `Ov23liXXX`
- **Client Secret**: `xxxxx`

Store these in .NET user secrets (see below).

---

## Storing OAuth Credentials in .NET User Secrets

### Why User Secrets?

**NEVER commit OAuth credentials to source control.** Use .NET User Secrets for local development and environment variables for production.

### Setup User Secrets

1. Navigate to the API project:
   ```bash
   cd apps/backend/src/Codewrinkles.API
   ```

2. Initialize user secrets (if not already done):
   ```bash
   dotnet user-secrets init
   ```

3. Store Google OAuth credentials:
   ```bash
   dotnet user-secrets set "OAuth:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
   dotnet user-secrets set "OAuth:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
   ```

4. Store GitHub OAuth credentials:
   ```bash
   dotnet user-secrets set "OAuth:GitHub:ClientId" "YOUR_GITHUB_CLIENT_ID"
   dotnet user-secrets set "OAuth:GitHub:ClientSecret" "YOUR_GITHUB_CLIENT_SECRET"
   ```

### Verify User Secrets

List all stored secrets:
```bash
dotnet user-secrets list
```

You should see:
```
OAuth:GitHub:ClientId = Ov23liXXX
OAuth:GitHub:ClientSecret = xxxxx
OAuth:Google:ClientId = xxx.apps.googleusercontent.com
OAuth:Google:ClientSecret = GOCSPX-xxx
```

---

## Production Deployment

For production, **DO NOT use user secrets**. Instead, use secure environment variables or a secrets manager.

### Azure App Service

Set application settings in the Azure portal:
- `OAuth__Google__ClientId`
- `OAuth__Google__ClientSecret`
- `OAuth__GitHub__ClientId`
- `OAuth__GitHub__ClientSecret`

### Docker/Kubernetes

Pass as environment variables:
```yaml
environment:
  - OAuth__Google__ClientId=xxx
  - OAuth__Google__ClientSecret=xxx
  - OAuth__GitHub__ClientId=xxx
  - OAuth__GitHub__ClientSecret=xxx
```

### GitHub Actions / CI/CD

Store as repository secrets and inject during deployment.

---

## Testing OAuth Flow

### Development URLs

- **Frontend**: `http://localhost:5173`
- **Backend API**: `https://localhost:7280`

### Testing Steps

1. Start the backend:
   ```bash
   cd apps/backend/src/Codewrinkles.API
   dotnet run
   ```

2. Start the frontend:
   ```bash
   cd apps/frontend
   npm run dev
   ```

3. Navigate to `http://localhost:5173/login`

4. Click "Continue with Google" or "Continue with GitHub"

5. Complete the OAuth flow

6. You should be redirected back to the app and logged in

---

## Troubleshooting

### "Invalid OAuth provider" error

- **Cause**: Client ID/Secret not configured
- **Fix**: Verify user secrets are set correctly

### "redirect_uri_mismatch" error

- **Cause**: Redirect URI in Google/GitHub doesn't match callback URL
- **Fix**: Ensure callback URLs match exactly:
  - Google: `https://localhost:7280/api/identity/oauth/google/callback`
  - GitHub: `https://localhost:7280/api/identity/oauth/github/callback`

### "Invalid state" error

- **Cause**: State parameter expired or tampered with
- **Fix**: Try again (state expires after 10 minutes)

### OAuth works but user not logged in

- **Cause**: JWT tokens not being stored correctly
- **Fix**: Check browser console for errors, verify `completeOAuthLogin` in useAuth hook

---

## Security Considerations

1. **CSRF Protection**: State parameter prevents CSRF attacks
2. **Token Security**: OAuth tokens stored securely in backend, never exposed to frontend
3. **HTTPS Required**: Always use HTTPS in production
4. **Scope Limitation**: Only request necessary scopes (openid, email, profile)
5. **Token Expiry**: Access tokens expire and are refreshed as needed

---

## OAuth Flow Summary

```
User clicks "Sign in with Google"
    ↓
Frontend: POST /api/identity/oauth/google/initiate
    ↓
Backend: Generates state, stores in cache, returns Google authorization URL
    ↓
Frontend: Redirects to Google consent page
    ↓
User grants permission
    ↓
Google redirects to: /api/identity/oauth/google/callback?code=xxx&state=xxx
    ↓
Backend: Validates state, exchanges code for tokens, creates user, generates JWT
    ↓
Backend redirects to: /auth/success?access_token=JWT&refresh_token=JWT&is_new_user=true
    ↓
Frontend: Stores JWT tokens, updates auth state, redirects to /onboarding or /pulse
```

---

## References

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [GitHub OAuth Documentation](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
