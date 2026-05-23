import { PublicClientApplication, Configuration, LogLevel } from '@azure/msal-browser';

/**
 * MSAL (Microsoft Authentication Library) configuration for Azure AD integration.
 *
 * Accor Azure AD tenant — uses SPA registration (APP-Expat-SPA-DEV):
 * - Tenant ID:   3ee81190-954b-4064-8e7d-f12fd761fd39
 * - SPA Client ID: 3024025c-2f2d-446d-8131-891e576f3927  (APP-Expat-SPA-DEV)
 * - API Client ID: 2b8799fd-16e3-4bb1-92a5-babd0a8d2cee  (APP-Expat-API-DEV, set in appsettings)
 * - Scopes: openid, profile, email (ID token exchanged for internal JWT at /api/v1/auth/azure-login)
 */

const msalConfig: Configuration = {
  auth: {
    clientId: '3024025c-2f2d-446d-8131-891e576f3927', // APP-Expat-SPA-DEV
    authority: 'https://login.microsoftonline.com/3ee81190-954b-4064-8e7d-f12fd761fd39', // Tenant
    redirectUri: window.location.origin, // e.g., http://localhost:5173
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    // localStorage persists across tab/browser restarts so acquireTokenSilent
    // finds the cached account on every visit after the first login.
    // sessionStorage would clear on tab close, forcing re-login every time.
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: true, // fallback for browsers that block third-party cookies
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message) => {
        // Only log warnings and errors
        if (level === LogLevel.Warning || level === LogLevel.Error) {
          console.log(`[MSAL] ${message}`);
        }
      },
      piiLoggingEnabled: false,
    },
  },
};

/**
 * Standard OIDC scopes for login. These never require admin consent or user approval screens.
 * The resulting ID token (audience = SPA Client ID) is sent to the backend for validation.
 */
export const apiScopes = ['openid', 'profile', 'email'];

/**
 * Initialize MSAL Public Client Application.
 * This is a singleton instance used throughout the app.
 */
export const msalInstance = new PublicClientApplication(msalConfig);
