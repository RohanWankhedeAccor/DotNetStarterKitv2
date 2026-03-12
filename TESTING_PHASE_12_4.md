# Phase 12.4 Testing: Logout & Token Refresh

**Status**: 🟢 Implementation Complete - Ready for Testing
**Date**: 2026-03-10
**Priority**: P1 (Blocking full Azure AD integration)

---

## Test Environment Setup

### Prerequisites
- Backend running: `dotnet run --project src/Api` (port 5031)
- Frontend running: `npm run dev` from src/Web (port 5173)
- Logged in with **local account** (admin@localhost)
- Browser DevTools open (F12 → Console tab)

### Known State
- Currently logged in as "Admin User" (local session from Phase 9)
- UserProfileMenu should now render with fallback values
- Logout button functionality just implemented

---

## Test 1: Logout Button Visibility ✅

**Objective**: Verify UserProfileMenu renders correctly for local login

### Steps
1. **Hard Refresh Browser**: `Ctrl+Shift+R`
2. **Locate Profile Section**: Look at sidebar top-left
   - Should show user avatar with initials
   - User name: "Admin User"
   - Email: "admin@localhost"
3. **Click Profile Section**: Dropdown menu should appear
4. **Verify Menu Items**:
   - ⚙️ Settings (Coming Soon) — enabled
   - 🚪 Logout — enabled

**Expected Result**: ✅ Menu appears with both options visible

**Failure Indicators**:
- ❌ UserProfileMenu doesn't appear at all
- ❌ Menu is grayed out / disabled
- ❌ Errors in browser console

---

## Test 2: Logout Functionality ✅

**Objective**: Verify logout clears auth state and redirects correctly

### Steps
1. **From Test 1 State**: Profile menu is open
2. **Click "Logout"** button
3. **Watch for Toast Notification**: Should see green toast:
   ```
   You have been logged out.
   ```
4. **Verify Redirect**: Page should redirect to home (`/`)
5. **Check Redux State**:
   - Open DevTools → Redux tab (if installed)
   - Check `auth.user` slice
   - All values should be null/empty

### Browser Console Checks
```javascript
// Open DevTools Console and paste:
// Should show logout was called
[MSAL] Silent SSO not available...  // ← Expected after logout
[SSO] No accounts available...      // ← Expected
```

**Expected Result**: ✅ Clean logout, toast appears, page redirects

**Failure Indicators**:
- ❌ Toast doesn't appear
- ❌ Page doesn't redirect
- ❌ Redux state still has user data
- ❌ Errors in console

---

## Test 3: Login with Azure AD (After Logout) ✅

**Objective**: Verify full Azure AD login flow after logout

### Prerequisites
- **You must be logged into Azure AD/Microsoft 365**:
  - Go to `https://portal.office.com`
  - Sign in with your Azure AD account (same tenant as configured)
  - Keep this session open in another tab

### Steps
1. **From Test 2 State**: You've just logged out
2. **Look for Login Options**:
   - If page shows login page: Click "Login with Azure AD" button
   - If page loaded as guest: Navigate to login/Auth endpoint
3. **MSAL Popup Appears**:
   - Microsoft login form should appear
   - Or it might auto-detect your Azure AD session (no popup needed)
4. **Authorize the App**:
   - If prompted: Click "Consent" or "Accept"
   - App requests: `api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user`
5. **Success Indicators**:
   - 🟢 Success toast: `Welcome, [Your Name]! You're now logged in with Azure AD.`
   - Redux state populated with Azure AD user info:
     ```typescript
     {
       userId: "...",
       email: "you@company.com",
       fullName: "Your Full Name",
       roles: [...],
       token: "eyJ0...",  // JWT token
       authSource: "AzureAd"
     }
     ```
   - Dashboard loads with your name in greeting
   - UserProfileMenu shows your Azure AD email

### Browser Console Checks
```javascript
// Should see successful token exchange log:
[SSO] Silent login successful for you@company.com  // ← Only if you had Azure session open
// OR, if you clicked button:
Azure AD login success...
Token exchanged successfully...
```

**Expected Result**: ✅ Full Azure AD login works, JWT issued, dashboard loaded

**Failure Indicators**:
- ❌ MSAL popup doesn't appear or closes without auth
- ❌ 401/400 error from `/api/v1/auth/azure-login`
- ❌ Toast shows error message
- ❌ Redux state not updated
- ❌ Dashboard doesn't load

---

## Test 4: Token Validation (Backend Check) 🔧

**Objective**: Verify backend correctly validates Azure AD token and issues JWT

### Steps

#### Option A: Manual Test with PowerShell Script
```powershell
# 1. Get Azure AD token using Azure CLI
az login
az account get-access-token --resource api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user

# 2. Copy the accessToken value

# 3. Run test script
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2
.\test-azure-login.ps1

# 4. Paste the token when prompted
```

#### Option B: Manual Test with Curl
```bash
# Get token
$token = (az account get-access-token --resource api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user).accessToken

# Test endpoint
curl -X POST https://localhost:5031/api/v1/auth/azure-login `
  -H "Content-Type: application/json" `
  -d @"
{
  "azureAdToken": "$token"
}
"@
```

### Expected Response
```json
HTTP 200 OK
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "you@company.com",
  "fullName": "Your Full Name",
  "roles": ["User", ...],
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

**Failure Cases**:
- ❌ HTTP 401: Token validation failed (check token expiration, scopes)
- ❌ HTTP 404: User not found (user needs to be provisioned)
- ❌ HTTP 500: Backend error (check API logs)

---

## Test 5: Token Expiration (Optional) ⏰

**Objective**: Verify JWT token lifecycle and refresh mechanism (Phase 12.5)

### Current Behavior
- JWT tokens valid for **1 hour** (3600 seconds)
- No auto-refresh yet (Phase 12.5)
- Manual login required after expiry

### How to Test (requires waiting)
```javascript
// In Console, calculate expiry:
const expiresAt = Date.now() + (token.expiresIn * 1000);
const minutesRemaining = ((expiresAt - Date.now()) / 1000 / 60).toFixed(2);
console.log(`Token expires in ${minutesRemaining} minutes`);
```

---

## Test 6: Silent SSO (Bonus) 🌟

**Objective**: Verify Silent SSO on app load when already in Azure AD session

### Preconditions
- You must be logged into **Azure AD in the same browser**
- Keep `https://portal.office.com` open in another tab
- Clear Redux state (logout first)

### Steps
1. **Reload App**: `Ctrl+R` on `localhost:5173`
2. **Observe Console Immediately**:
   - Within 2-3 seconds, should see:
     ```
     [SSO] Silent login successful for you@company.com
     ```
3. **Check Dashboard**: Should load with your Azure AD name without clicking anything

**Expected Result**: ✅ Auto-login without any user interaction

**Notes**:
- Only works if you have an **active Azure AD session** in this browser
- If no session: Shows `[SSO] No accounts available` → manual login required
- This is the seamless UX we built for

---

## Debugging Guide

### Issue: Logout Button Doesn't Appear
**Possible Causes**:
1. Browser cache not cleared
   - **Fix**: `Ctrl+Shift+R` (hard refresh)
2. Component not rendering
   - **Check**: DevTools → Elements → Find UserProfileMenu
   - **Fix**: Check browser console for TypeScript errors
3. Redux not initialized
   - **Fix**: Check Redux DevTools (if installed)

### Issue: Logout Button Appears but Clicking Does Nothing
**Possible Causes**:
1. useAzureLogin hook error
   - **Check**: `console.error('Logout error:')` in console
2. MSAL instance not ready
   - **Fix**: Wait for app to fully load
3. Event handler not attached
   - **Check**: Right-click button → Inspect → Check if onClick is there

### Issue: Logout Works but Page Doesn't Redirect
**Possible Causes**:
1. Redirect happens too fast
   - **Check**: Redux state cleared? (Yes = working)
   - **Fix**: Page redirects to `/` which reloads the app
2. Router issue
   - **Note**: App uses state-based nav, not React Router for main views
   - **Expected**: Full page reload via `window.location.href = '/'`

### Issue: Azure AD Login Returns 401
**Possible Causes**:
1. Token expired (get fresh one with `az account get-access-token`)
2. Tenant ID mismatch (check msalConfig.ts)
3. Scopes incorrect (should be `api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user`)
4. App registration not configured correctly

**Debug Steps**:
```bash
# Check token claims
$token = "paste-your-token-here"
$payload = $token.Split('.')[1]
# Pad to multiple of 4
while ($payload.Length % 4) { $payload += "=" }
[System.Convert]::FromBase64String($payload) | % { [System.Text.Encoding]::UTF8.GetString($_) }
```

### Issue: User Not Found (404) on First Azure AD Login
**Expected Behavior**: User should be auto-provisioned from Azure AD claims
- Check API logs for error details
- Verify user provisioning logic in AzureLoginCommandHandler.cs

---

## Success Checklist ✅

- [ ] UserProfileMenu appears with local user name
- [ ] Logout button visible and clickable
- [ ] Clicking logout shows success toast
- [ ] Redux auth state clears after logout
- [ ] Page redirects to home after logout
- [ ] Silent SSO works (if you have Azure session)
- [ ] Manual Azure AD login works (popup or redirect)
- [ ] Backend validates token correctly (HTTP 200)
- [ ] JWT token issued and stored in Redux
- [ ] Dashboard loads with Azure AD user name

---

## Next Steps (Phase 12.5+)

- [ ] **Token Refresh**: Auto-refresh JWT before 1-hour expiry
- [ ] **Loading Spinner**: Show spinner during Silent SSO
- [ ] **Better Errors**: Improve error messages for failed logins
- [ ] **Session Persistence**: Save user data to localStorage
- [ ] **Logout Confirmation**: Confirm logout before clearing data
- [ ] **Multi-tab Sync**: Handle logout in one tab, sync across other tabs

---

## Files Modified (Phase 12.4)

- `src/Web/src/features/auth/components/UserProfileMenu.tsx` — Added logout dropdown
- `src/Web/src/features/auth/hooks/useAzureLogin.ts` — Added logoutFromAzureAd function
- `src/Web/src/App.tsx` — Integrated UserProfileMenu in sidebar
- Database migrations — No changes needed

---

## Quick Start Commands

```bash
# Terminal 1: Start Backend
cd D:\RWANKHEDE\Claude\DotNetStarterKitv2
dotnet run --project src/Api

# Terminal 2: Start Frontend
cd src/Web
npm run dev

# Terminal 3 (Optional): Watch backend logs
Get-Content ./build.log -Tail 20 -Wait
```

Then open `http://localhost:5173` and test!

