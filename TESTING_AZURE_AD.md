# Azure AD Integration Testing Guide (Phase 12.3)

**Last Updated:** March 10, 2026
**Status:** Phase 12.1-2 Complete | Phase 12.3 Testing In Progress

---

## 🎯 Testing Overview

This document covers **end-to-end testing** of the Azure AD integration for internal employees (Part 1).

### Test Environments
- **Backend API:** `http://localhost:5031` (HTTP, local dev)
- **Frontend SPA:** `http://localhost:5173` or `http://localhost:5174` (Vite dev server)
- **Azure AD Tenant:** `3ee81190-954b-4064-8e7d-f12fd761fd39`
- **Database:** `DotNetStarterKit` (SQL Server Express)

---

## 🧪 Test Scenarios

### **Test 1: Manual Azure AD Login (Explicit)**
**Objective:** Verify users can manually log in via Azure AD

**Steps:**
1. Start backend: `dotnet run --project src/Api`
2. Start frontend: `npm run dev` (in src/Web)
3. Navigate to `http://localhost:5173`
4. Click "**Login with Azure AD**" button
5. In popup, enter your Microsoft 365 / Azure AD credentials
6. Authorize the app to access your profile
7. Should see welcome message and redirect to dashboard

**Expected Result:** ✅
- MSAL popup closes
- Token exchange succeeds (200 OK from `/api/v1/auth/azure-login`)
- Redux state shows: `userId`, `email`, `fullName`, `roles`, `token`
- User redirected to dashboard
- Sidebar shows user name + email

**Failure Points:**
- ❌ Popup blocked → Check browser popup settings
- ❌ 401 from `/api/v1/auth/azure-login` → Token validation failed
- ❌ 404 for user → User not provisioned (check backend logs)
- ❌ Roles empty → User not assigned to any roles in database

---

### **Test 2: Silent SSO (Automatic Login)**
**Objective:** Verify users auto-login if already in Azure AD session

**Steps:**
1. Log in to `https://portal.office.com` in your browser (or any Microsoft service)
2. Close frontend app completely
3. Start frontend fresh: `npm run dev`
4. Navigate to `http://localhost:5173`
5. **Do NOT click login button**
6. Wait 2-3 seconds

**Expected Result:** ✅
- No popup appears
- `useSilentSso` runs in background
- `acquireTokenSilent` succeeds (silent, no UI)
- Token exchange happens automatically
- Dashboard loads **without user clicking anything**
- Browser console logs: `[SSO] Silent login successful for user@email.com`

**Failure Points:**
- ❌ No auto-login → Check browser console for `[SSO]` logs
- ❌ Silent SSO available but 401 → Backend token validation issue
- ❌ User already logged out of Azure AD → Silent flow expected to fail (user clicks button)

**Browser Console Output (Success):**
```
[SSO] Silent login successful for user@example.com
```

---

### **Test 3: Backend Token Validation**
**Objective:** Verify backend correctly validates Azure AD tokens

**Curl Commands:**

**Step A: Get Azure AD token (manual)**
```bash
# Use Azure CLI
az account get-access-token --resource api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user
# Returns: { "accessToken": "eyJ0eXAi..." }
```

**Step B: Exchange token for internal JWT**
```bash
curl -X POST http://localhost:5031/api/v1/auth/azure-login \
  -H "Content-Type: application/json" \
  -d '{
    "azureAdToken": "eyJ0eXAi..."
  }'
```

**Expected Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "fullName": "John Doe",
  "roles": ["Employee", "Admin"],
  "expiresIn": 3600
}
```

**Failure Points:**
- ❌ 401 Unauthorized → Invalid or expired Azure AD token
- ❌ 400 Bad Request → Missing `azureAdToken` in request body
- ❌ 404 Not Found → User not provisioned in database
- ❌ 500 Internal Error → Backend exception (check logs)

**Backend Logs (Success):**
```
[INFO] Azure AD token validation succeeded for user: user@example.com
[INFO] User provisioned/synced: userId=550e8400...
```

---

### **Test 4: User Provisioning from Azure AD**
**Objective:** Verify new Azure AD users are auto-created in database

**Setup:**
1. Use **new Azure AD user** (never logged in before)
2. Follow Test 1: Manual Azure AD Login

**Expected Result:** ✅
- User doesn't exist in database yet
- Backend creates new User record:
  - Email from Azure AD
  - Full name from Azure AD
  - `AzureAdObjectId` = Azure AD OID
  - `AuthSource` = "AzureAd"
  - Status = Active (auto-activate Azure AD users)
  - No password hash
- User can see dashboard
- Query database to verify:
  ```sql
  SELECT * FROM Users WHERE Email = 'newemail@example.com'
  ```

**Expected Database State:**
| Column | Value |
|--------|-------|
| Email | newemail@example.com |
| FullName | New User Name |
| PasswordHash | NULL |
| Status | 1 (Active) |
| AzureAdObjectId | {Azure OID} |
| AuthSource | AzureAd |
| IsDeleted | 0 |

---

### **Test 5: User Sync from Azure AD**
**Objective:** Verify existing users are updated with latest Azure AD info

**Setup:**
1. Log in first time with User A → creates database record
2. Change User A's full name in Azure AD (or use different account)
3. Log out
4. Log in again with same user

**Expected Result:** ✅
- Backend fetches user by `AzureAdObjectId`
- User profile updated (email, name)
- Same `userId` returned (no new user created)
- Check database: Full name is updated

---

### **Test 6: Invalid Token Rejection**
**Objective:** Verify backend rejects invalid/expired tokens

**Steps:**
```bash
# Try with invalid token
curl -X POST http://localhost:5031/api/v1/auth/azure-login \
  -H "Content-Type: application/json" \
  -d '{
    "azureAdToken": "invalid.token.here"
  }'
```

**Expected Response (401 Unauthorized):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Azure AD Token Validation Failed",
  "status": 401,
  "detail": "Token signature is invalid.",
  "traceId": "0HN4B6Q7V8S9K2L3M4N5O6P7Q8R9S0T1"
}
```

---

### **Test 7: Role-Based Authorization**
**Objective:** Verify roles are correctly extracted and returned

**Setup:**
1. In database, assign user to roles:
   ```sql
   INSERT INTO UserRoles (UserId, RoleId) VALUES
   ('550e8400...', 1), -- 'Admin' role
   ('550e8400...', 2)  -- 'Employee' role
   ```
2. Log in with that user

**Expected Response:**
```json
{
  "roles": ["Admin", "Employee"],
  ...
}
```

**Frontend Redux State:**
```typescript
state.auth.user.roles === ["Admin", "Employee"]
```

---

### **Test 8: Token Expiration**
**Objective:** Verify tokens expire after 1 hour

**Steps:**
1. Log in at **Time T**
2. Check token in Redux: `expiresIn: 3600`
3. Wait 1 hour (or modify JwtTokenService to shorter expiry for testing)
4. Try to use expired token on protected endpoint

**Expected Result:** ✅
- Token still valid for 1 hour from login
- After expiration, need to re-login
- Backend rejects expired tokens (401)

---

### **Test 9: Logout**
**Objective:** Verify logout clears session

**Steps:**
1. Log in successfully
2. Click "**Logout**" button (when implemented)
3. Or manually dispatch logout action

**Expected Result:** ✅
- MSAL clears browser session
- Redux state cleared: `token = null`
- Redirect to login page
- User cannot access dashboard without re-login

---

### **Test 10: Concurrent Requests with Token**
**Objective:** Verify token is sent in all API requests

**Setup:**
1. Log in with Azure AD
2. Open browser DevTools → Network tab
3. Navigate to dashboard → Users, Products, etc.

**Expected:** ✅
- Every request includes: `Authorization: Bearer eyJ0eXAi...`
- All requests succeed (200 OK)
- If token removed → requests return 401

---

## 🔧 Troubleshooting

### Issue: "MSAL popup blocked"
**Solution:**
- Check browser popup settings
- Whitelist `localhost:5173` and `localhost:5174`
- Disable popup blocker temporarily for testing

### Issue: "401 Unauthorized from Azure login endpoint"
**Solution:**
1. Check token is valid: `az account get-access-token`
2. Check backend logs for validation error
3. Verify Tenant ID, Client ID match Azure AD app registration
4. Check token scopes: Should include `api://ae60f82b.../access_as_user`

### Issue: "User not found (404)"
**Solution:**
1. Check database for user record:
   ```sql
   SELECT * FROM Users WHERE Email = 'user@example.com'
   ```
2. If missing → user not provisioned, check backend logs
3. If exists with wrong status → update: `UPDATE Users SET Status = 1 WHERE ...`

### Issue: "Silent SSO not working"
**Solution:**
1. Open browser console → search for `[SSO]`
2. If missing → Silent SSO didn't run (check AppInitializer mounted)
3. If log says "Silent SSO not available" → User not in Azure AD session, manual login required
4. Check MSAL config: scopes, clientId, authority

### Issue: "Redux state not updating"
**Solution:**
1. Check Redux DevTools (install browser extension)
2. Verify `setUser` action dispatched
3. Check action payload has all fields
4. Verify store initialized in main.tsx with Provider

---

## ✅ Sign-Off Checklist

- [ ] Test 1: Manual Azure AD Login ✅
- [ ] Test 2: Silent SSO ✅
- [ ] Test 3: Backend Token Validation ✅
- [ ] Test 4: User Provisioning ✅
- [ ] Test 5: User Sync ✅
- [ ] Test 6: Invalid Token Rejection ✅
- [ ] Test 7: Role-Based Auth ✅
- [ ] Test 8: Token Expiration ✅
- [ ] Test 9: Logout ✅
- [ ] Test 10: Concurrent Requests ✅

**Phase 12.3 Complete When:** All tests pass ✅

---

## 📋 Next Steps (Phase 12.4: Polish)

- [ ] Add logout button to UI
- [ ] Implement token refresh (before 1-hour expiry)
- [ ] Add loading spinner during Silent SSO
- [ ] Create error messages for failed Azure login
- [ ] Update README with Azure AD setup instructions
- [ ] Document scope requirements for different roles
- [ ] Add audit logging for login events

---

## 📞 Support

For issues with Azure AD or MSAL:
- Azure AD docs: https://learn.microsoft.com/en-us/azure/active-directory/
- MSAL.js docs: https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-browser-use-cases
- Backend token validation: Check `src/Infrastructure/Identity/AzureAdTokenValidator.cs`
