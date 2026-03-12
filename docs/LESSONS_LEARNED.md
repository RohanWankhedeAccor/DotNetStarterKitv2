# Lessons Learned — DotNetStarterKitv2

A living document capturing real bugs, root causes, and fixes encountered during development.
Updated as we go. 🤝

---

## Authentication — Azure AD Integration (Phase 12)

> 2 days, 4 rounds, 1 working login. Here's every wall we hit.

---

### Lesson 1 — Corporate Tenants Block Custom API Scopes

**Date:** 2026-03-12
**Error:**
```
AADSTS65004: User declined to consent to access the app.
```

**Root Cause:**
Switched MSAL scope from `User.Read` to the custom API scope
`api://ae60f82b.../access_as_user`. The Accor corporate tenant (`accor.com`)
enforces **admin consent required** for all custom API permissions. The "consent"
popup appeared and the user didn't realize they had to click Accept — or couldn't,
because the tenant admin must pre-approve it.

**Fix:**
Use OIDC scopes (`openid profile email`) for login. These are standard OAuth2
scopes that **never require admin consent** and are always available.

```typescript
// msalConfig.ts
export const apiScopes = ['openid', 'profile', 'email'];
```

**Rule for the future:**
> Always start with `openid profile email` in corporate environments.
> Switch to a custom API scope only after an admin grants consent in Azure Portal:
> App Registration → API Permissions → Grant admin consent.

---

### Lesson 2 — Access Tokens Are Not for Third-Party Validation

**Date:** 2026-03-12
**Error:**
```
Token signature is invalid.
```

**Root Cause:**
We were sending the **access token** (`tokenResponse.accessToken`) to our backend
for validation. Graph API access tokens are **opaque tokens** — Microsoft signs
them with internal keys that are NOT the same as the OIDC public keys published at
`.well-known/openid-configuration`. Only Microsoft's own Graph service can validate
them. Third parties that try to validate them will always get a signature mismatch.

**Fix:**
Send the **ID token** (`loginResponse.idToken`) instead. ID tokens are designed
specifically for the relying party (your backend) to validate. They are always
signed with the standard OIDC keys.

```typescript
// useAzureLogin.ts — BEFORE (wrong)
const tokenResponse = await instance.acquireTokenSilent(accessTokenRequest);
const azureAdToken = tokenResponse.accessToken;  // ❌ Graph token, not validatable

// AFTER (correct)
const azureAdToken = loginResponse.idToken;  // ✅ ID token, meant for us
```

**Rule for the future:**
> **Access token** = prove what you can DO to a resource (Graph, your API).
> **ID token** = prove WHO YOU ARE to your app.
> Token exchange login patterns always use the ID token.

---

### Lesson 3 — JwtSecurityTokenHandler Silently Remaps Claim Names

**Date:** 2026-03-12
**Error:**
```
Azure AD token does not contain 'oid' claim.
```

**Root Cause:**
`JwtSecurityTokenHandler` has a built-in `InboundClaimTypeMap` that transforms
short JWT claim names to long .NET CLR URI strings by default:

| JWT claim | Becomes (default) |
|---|---|
| `oid` | `http://schemas.microsoft.com/identity/claims/objectidentifier` |
| `sub` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` |
| `email` | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` |

Calling `principal.FindFirst("oid")` returns **null** even though the claim is
present — because it was renamed to the long URI under the hood.

**Fix:**
Set `MapInboundClaims = false` on the token handler. All JWT claims keep their
original short names.

```csharp
// AzureAdTokenValidator.cs
_tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
```

Then use raw JWT claim names everywhere:

```csharp
// AzureLoginCommandHandler.cs — BEFORE (wrong)
var email = GetClaimValue(principal, ClaimTypes.Email);  // ❌ long URI, won't match

// AFTER (correct)
var email = GetClaimValue(principal, "email")           // ✅ raw JWT name
         ?? GetClaimValue(principal, "preferred_username");
```

**Rule for the future:**
> Always set `MapInboundClaims = false` in modern .NET (6+). It is the
> recommended default going forward. Never mix `ClaimTypes.*` URIs with
> raw JWT claim names in the same validator.

---

### Lesson 4 — "Check Then Act" DB Patterns Are Race Conditions

**Date:** 2026-03-12
**Error:**
```
Cannot insert duplicate key row in object 'dbo.Users'
with unique index 'IX_Users_Email'. (Rohan.WANKHEDE@accor.com)
```

**Root Cause:**
`useSilentSso` (runs on app load) and the manual login popup both fired
simultaneously, both posting to `/api/v1/auth/azure-login` at the same time.

Timeline:
```
Request A → SELECT by AzureAdObjectId → NULL (user doesn't exist)
Request B → SELECT by AzureAdObjectId → NULL (user doesn't exist)
Request A → INSERT new user           → SUCCESS ✅
Request B → INSERT new user           → DUPLICATE KEY 💥
```

Classic **check-then-act** race condition. The SELECT and INSERT are not atomic.

**Fix (3 layers):**

1. Secondary lookup by email before inserting — handles existing local accounts:
```csharp
var user = await FindByAzureAdObjectId(oid)
        ?? await FindByEmail(email);   // ← new fallback
```

2. Wrap INSERT in try/catch — recover from concurrent inserts gracefully:
```csharp
try {
    await _context.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateException) {
    // Concurrent insert — re-query instead of crashing
    user = await FindByEmail(email) ?? throw ...;
}
```

3. Bonus: adopts existing local accounts — if a user registered with email/password
   before the Azure AD integration, their account is linked rather than duplicated.

**Rule for the future:**
> Never assume a SELECT-then-INSERT is safe under concurrent load.
> Always either use a unique constraint + catch, or an UPSERT pattern.
> The DB constraint is your safety net — let it fire, then recover.

---

## API Client — JWT Token Not Sent in Requests

**Date:** 2026-03-12
**Symptom:** User logs in successfully, dashboard loads, but API calls go out
unauthenticated (no `Authorization` header).

**Root Cause:**
The JWT token was stored in Redux after login, but `api-client.ts` had no request
interceptor to attach it. Every Axios request went out as anonymous.

**Fix:**
Add a request interceptor that reads the token from the Redux store singleton:

```typescript
// api-client.ts
import { store } from './redux/store'

apiClient.interceptors.request.use((config) => {
  const token = store.getState().auth.user.token
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})
```

**Rule for the future:**
> When using Redux + Axios, always wire the request interceptor at setup time.
> Import the store directly — it's a singleton, no circular dependency risk.

---

## General Patterns

### Azure AD App Registration Checklist
When setting up a new Azure AD app:
- [ ] Redirect URI type = **Single-page application (SPA)** (enables PKCE, disables implicit flow)
- [ ] Leave both Implicit grant checkboxes **UNCHECKED**
- [ ] Token configuration → add optional claims: `email`, `preferred_username`, `upn`
- [ ] API Permissions → grant admin consent for corporate tenants
- [ ] Two registrations: one for the SPA (frontend), one for the API (backend)

### Two-App Registration Pattern
```
SPA App (a51f9d7f...)          API App (ae60f82b...)
├── Redirect URI: localhost     ├── Expose API scope: access_as_user
├── API Permissions:            └── App ID URI: api://ae60f82b...
│   └── access_as_user
└── No client secret needed
```

### Token Flow (our hybrid pattern)
```
MSAL loginPopup()
  → ID token (aud = SPA Client ID)
    → POST /api/v1/auth/azure-login
      → backend validates ID token
      → provisions/syncs user in DB
      → issues internal JWT
        → stored in Redux
          → attached to all API calls via interceptor
```

---

*Last updated: 2026-03-12*
*Authors: Rohan + Claude — Team effort 🤝*
