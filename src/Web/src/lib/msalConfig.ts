import { PublicClientApplication, Configuration, LogLevel } from '@azure/msal-browser';

/**
 * MSAL (Microsoft Authentication Library) configuration for Azure AD integration.
 * Phase 12: Frontend Azure AD login setup.
 *
 * Configuration details:
 * - Tenant ID: 3ee81190-954b-4064-8e7d-f12fd761fd39
 * - SPA Client ID: a51f9d7f-eec3-403d-af05-18d54a18f248
 * - Authority: https://login.microsoftonline.com/{tenantId}
 * - Scopes: api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user
 */

const msalConfig: Configuration = {
  auth: {
    clientId: 'a51f9d7f-eec3-403d-af05-18d54a18f248', // SPA Client ID
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
 * Scope for acquiring an access token addressed to our API.
 * The audience of this token is `api://{apiClientId}`, which is what the
 * backend's AzureAdTokenValidator now accepts (access token, not ID token).
 */
export const apiScopes = ['api://ae60f82b-2dc4-4212-884a-3a50d79bb768/access_as_user'];

/**
 * Initialize MSAL Public Client Application.
 * This is a singleton instance used throughout the app.
 */
export const msalInstance = new PublicClientApplication(msalConfig);
