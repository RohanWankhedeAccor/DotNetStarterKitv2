# Azure AD Silent SSO Testing Guide

## Current Status ✅

| Component | Status | Details |
|-----------|--------|---------|
| **API Server** | ✅ Running | http://localhost:5031 |
| **Web Server** | ✅ Running | http://localhost:5173 |
| **Vite Proxy** | ✅ Configured | `/api` → `http://localhost:5031` |
| **MSAL Config** | ✅ Loaded | Tenant: `3ee81190-954b-4064-8e7d-f12fd761fd39` |
| **Redux Auth** | ✅ Ready | State management configured |

## Azure AD Silent SSO Flow

### What Happens When You Open http://localhost:5173

```
1. App Loads
   └─ MsalProvider initializes with MSAL config
   └─ AppInitializer component mounts
   └─ useSilentSso() hook executes

2. Silent Token Acquisition
   ├─ Check if user already authenticated in Redux (isAuthenticated)
   ├─ Check if accounts exist in MSAL (accounts.length > 0)
   ├─ Try to silently acquire token from Azure AD
   │  └─ If NO Azure AD session: Silent SSO fails gracefully
   │  └─ If Azure AD session EXISTS: Get access token silently
   └─ Log: "[SSO] Silent SSO not available, manual login required"

3. Token Exchange (if token acquired)
   ├─ POST /api/v1/auth/azure-login with azureAdToken
   ├─ Backend validates token cryptographically
   ├─ Backend extracts claims (oid, email, name)
   ├─ Backend provisions user in database
   ├─ Backend generates internal JWT
   └─ Response: { token, userId, email, fullName, roles, expiresIn }

4. Redux State Update
   ├─ dispatch(setUser({...loginResponse}))
   ├─ authSlice.isAuthenticated = true
   ├─ User can now access protected routes
   └─ Token included in subsequent API calls

5. Manual Login Fallback
   ├─ If silent SSO fails, user sees dashboard
   ├─ AzureLoginButton visible
   └─ Click "Login with Azure AD" to trigger interactive login
```

## Testing Scenarios

### Scenario 1: No Azure AD Session (Most Likely)
**What happens:** Silent SSO fails gracefully
- Browser console: `[SSO] Silent SSO not available, manual login required`
- Dashboard loads but unauthenticated
- "Login with Azure AD" button is visible
- Click button to open login popup

**Expected result:** User can manually login with Azure AD credentials

### Scenario 2: Active Azure AD Session
**What happens:** Automatic silent login
- Browser console: `[SSO] Silent login successful for user@example.com`
- User object stored in Redux
- Dashboard shows user profile
- No login required

**Expected result:** Seamless authentication without user interaction

### Scenario 3: Token Exchange Fails
**What happens:** Backend validation error
- Browser console: `[SSO] Token exchange failed: {error details}`
- Redux auth state remains empty
- Dashboard loads unauthenticated

**Expected result:** User must manually login

## How to Test

### 1. Open Browser Developer Tools
```
Open http://localhost:5173
Press F12 to open DevTools
Go to Console tab
```

### 2. Monitor Silent SSO
Look for logs:
```
[SSO] No accounts available, user must login manually
[SSO] Silent login successful for user@example.com
[SSO] Silent SSO not available, manual login required
```

### 3. Check Redux State
Open Redux DevTools (if installed):
```
Look for authSlice state
Check: isAuthenticated flag
Check: user object (userId, email, fullName, token, roles)
```

### 4. Monitor Network Requests
In Network tab:
```
Watch for POST /api/v1/auth/azure-login
Success: 200 OK with JWT token
Failure: 401 Unauthorized with validation error
```

### 5. Test Manual Login
```
Click "Login with Azure AD" button
Follow popup authentication flow
Verify token exchange succeeds
Check Redux state updates with user info
```

## API Endpoints Reference

### Azure AD Token Exchange
```
POST /api/v1/auth/azure-login
Content-Type: application/json

Request:
{
  "azureAdToken": "<Azure AD JWT from MSAL.js>"
}

Success Response (200 OK):
{
  "token": "<internal JWT>",
  "userId": 123,
  "email": "user@example.com",
  "fullName": "John Doe",
  "roles": ["User", "Admin"],
  "expiresIn": 3600
}

Error Response (401 Unauthorized):
{
  "title": "Azure AD Token Validation Failed",
  "status": 401,
  "detail": "Failed to validate Azure AD token: <reason>",
  "traceId": "..."
}
```

## Debugging

### Issue: "Silent SSO not available"
**Cause:** No active Azure AD session in browser
**Solution:**
- Sign in to Azure AD first at https://portal.azure.com
- Or click manual "Login with Azure AD" button

### Issue: "Token exchange failed"
**Cause:** Azure AD token validation failed
**Solution:**
- Check token signature (validate against Azure AD public keys)
- Check token audience matches API Client ID
- Check token issuer matches tenant
- Check token expiration

### Issue: User not created in database
**Cause:** First-time login provisioning issue
**Solution:**
- Check database connection (appsettings.Development.json)
- Run EF Core migrations: `dotnet ef database update`
- Check AzureAdObjectId mapping

## Configuration

**Frontend MSAL Config** (`src/Web/src/lib/msalConfig.ts`):
- Tenant ID: `3ee81190-954b-4064-8e7d-f12fd761fd39`
- SPA Client ID: `a51f9d7f-eec3-403d-af05-18d54a18f248`
- API Scope: `api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user`

**Backend Azure AD Config** (`src/Api/appsettings.Development.json`):
- Tenant ID: `3ee81190-954b-4064-8e7d-f12fd761fd39`
- API Client ID: `ae60f82b-2dc4-4212-884a-3a50d79bb768`
- Authority: `https://login.microsoftonline.com/3ee81190-954b-4064-8e7d-f12fd761fd39`

## Next Steps

- [ ] Test with active Azure AD session
- [ ] Verify silent SSO succeeds
- [ ] Test token exchange endpoint
- [ ] Test manual login flow
- [ ] Verify Redux state persistence
- [ ] Test API calls with JWT token
- [ ] Test logout flow
- [ ] Test token refresh (before expiration)
