# Azure AD Integration - Quick Start Testing

**Phase 12 Status:** Backend (Phase 12.1) ✅ | Frontend (Phase 12.2) ✅ | Testing (Phase 12.3) 🟢

---

## 🚀 Quick Start (5 minutes)

### Prerequisites
- Backend running: `dotnet run --project src/Api`
- Frontend running: `npm run dev` (from src/Web)
- Logged into `https://portal.office.com` or any Microsoft 365 service

### Test 1: Silent SSO (Automatic Login)
**Expected: Auto-login without clicking anything**

1. Open browser DevTools → Console tab
2. Navigate to `http://localhost:5173`
3. Wait 2-3 seconds
4. Look for console log: `[SSO] Silent login successful for user@example.com`
5. Dashboard should load automatically ✅

### Test 2: Manual Login (If Silent SSO Doesn't Work)
**Expected: Popup login flow**

1. If dashboard doesn't load, click **"Login with Azure AD"** button
2. Microsoft login popup appears
3. Enter your Microsoft 365 credentials
4. Authorize the app
5. Popup closes, dashboard loads ✅

### Test 3: Verify Token Exchange
**Expected: Backend successfully validates Azure AD token**

1. Open DevTools → Network tab
2. Look for request to `POST /api/v1/auth/azure-login`
3. Response status should be `200 OK` with JWT token ✅

---

## 🔍 Debugging

### Silent SSO Not Working?
**Check browser console:**
```javascript
// Look for these logs:
[SSO] Silent login successful for user@example.com    // ✅ Success
[SSO] Silent SSO not available, manual login required // ⚠️ Expected if not in Azure AD session
```

### Login Button Click Not Working?
**Check browser console for errors:**
- MSAL errors: `[MSAL]` prefix
- Network errors: `POST /api/v1/auth/azure-login` failed
- Redux errors: Check Redux DevTools

### Backend Rejects Token?
**Check backend logs:**
```
[ERROR] Azure AD Token Validation Failed: Token signature is invalid
```

**Solutions:**
- Token may be expired (get fresh one with `az account get-access-token`)
- Tenant ID/Client ID mismatch (verify in `msalConfig.ts`)
- Scopes incorrect (should be `api://ae60f82b.../access_as_user`)

---

## 📊 Expected Behavior

### On First Visit (New Browser/Session)
```
Browser loads app
    ↓
AppInitializer runs Silent SSO
    ↓
MSAL checks for existing Azure AD session
    ↓
If session exists → Silent token fetch (no popup)
    ↓
Exchange token for internal JWT
    ↓
Redux: User state populated
    ↓
Dashboard loads with user context
```

### If Not in Azure AD Session
```
Browser loads app
    ↓
Silent SSO attempt fails (expected)
    ↓
User sees login page with "Login with Azure AD" button
    ↓
User clicks button
    ↓
MSAL popup login flow begins
    ↓
... (rest same as above)
```

---

## 🧪 Full Test Checklist

See `TESTING_AZURE_AD.md` for comprehensive test scenarios:

- [ ] **Test 1:** Manual Azure AD Login
- [ ] **Test 2:** Silent SSO
- [ ] **Test 3:** Backend Token Validation (Curl/PowerShell)
- [ ] **Test 4:** User Provisioning
- [ ] **Test 5:** User Sync
- [ ] **Test 6:** Invalid Token Rejection
- [ ] **Test 7:** Role-Based Authorization
- [ ] **Test 8:** Token Expiration
- [ ] **Test 9:** Logout
- [ ] **Test 10:** Concurrent API Requests

---

## 📝 Test Scripts

### PowerShell - Test Backend Endpoint
```bash
.\test-azure-login.ps1
```

This script:
1. Prompts for Azure AD access token
2. Sends to `/api/v1/auth/azure-login`
3. Shows response details (user, roles, token)
4. Useful for testing without frontend

### Azure CLI - Get Test Token
```bash
az login
az account get-access-token --resource api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user
```

Copy the `accessToken` value and use in PowerShell script or Curl.

---

## 🎯 Success Criteria

✅ All tests pass when:

1. **Manual Login Works**
   - Click button → Popup appears → Login succeeds → Dashboard loads

2. **Silent SSO Works**
   - App loads → Dashboard appears (no button click) → User info populated

3. **Backend Validates Tokens**
   - Valid token → 200 OK + JWT returned
   - Invalid token → 401 Unauthorized + error message

4. **User Provisioning Works**
   - New Azure AD users → Created in database automatically
   - Existing users → Synced with latest Azure AD info

5. **Authorization Works**
   - Roles fetched from database
   - Included in JWT token
   - Can use for role-based access control (RBAC)

---

## 🚀 Next: Phase 12.4 Polish

After testing succeeds:

- [ ] Add logout button to UI
- [ ] Implement token refresh before expiry
- [ ] Add loading spinner during Silent SSO
- [ ] Better error messages for failed login
- [ ] Update README with Azure AD setup guide
- [ ] Document configuration for different environments

---

## 📚 Additional Resources

- **Azure AD Docs:** https://learn.microsoft.com/azure/active-directory/
- **MSAL.js Docs:** https://learn.microsoft.com/azure/active-directory/develop/msal-browser-use-cases
- **JWT Tokens:** https://jwt.io/ (debug tokens)
- **Swagger API Docs:** http://localhost:5031/swagger

---

## ❓ Troubleshooting

| Issue | Solution |
|-------|----------|
| MSAL popup blocked | Whitelist localhost:5173 in popup blocker |
| 401 from login endpoint | Check token is valid, scopes correct |
| User not found (404) | User not provisioned, check backend logs |
| Silent SSO not working | Check browser console for `[SSO]` logs |
| Redux state not updated | Check Redux DevTools, verify action dispatched |
| API requests return 401 | Token may be expired, re-login required |

---

## 💡 Quick Commands

```bash
# Start backend
dotnet run --project src/Api

# Start frontend
cd src/Web && npm run dev

# Check API is running
curl http://localhost:5031/health

# View Swagger docs
open http://localhost:5031/swagger

# Test login endpoint (after getting Azure AD token)
.\test-azure-login.ps1
```

---

**Ready to test? Start with Test 1 in Quick Start above!** 🟢
