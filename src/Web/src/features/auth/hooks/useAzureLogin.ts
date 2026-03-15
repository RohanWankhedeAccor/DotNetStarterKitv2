import { useState } from 'react';
import { useDispatch } from 'react-redux';
import { useMsal } from '@azure/msal-react';
import { apiScopes } from '../../../lib/msalConfig';
import { clearUser } from '../../../lib/redux/authSlice';
import { exchangeAzureToken } from '../exchangeAzureToken';
import apiClient from '../../../lib/api-client';
import { toast } from 'sonner';

/**
 * Hook for Azure AD authentication.
 * Handles MSAL login, token exchange, and Redux state management.
 * Part of Phase 12: Azure AD Integration (Part 1).
 *
 * Flow:
 * 1. User clicks "Login with Azure AD"
 * 2. MSAL shows Microsoft login popup
 * 3. User authenticates with Azure AD
 * 4. Get Azure AD token from MSAL
 * 5. POST to /api/v1/auth/azure-login with token
 * 6. Backend validates token, creates/syncs user, returns JWT
 * 7. Store JWT in Redux
 * 8. Redirect to dashboard
 */
export const useAzureLogin = () => {
  const dispatch = useDispatch();
  const { instance } = useMsal();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loginWithAzureAd = async () => {
    setIsLoading(true);
    setError(null);

    try {
      // Step 1: Initiate MSAL login popup
      const loginResponse = await instance.loginPopup({
        scopes: apiScopes,
        prompt: 'select_account',
      });

      // Step 2: Use the ID token — signed for our SPA Client ID.
      // Standard OIDC scopes never trigger admin consent screens.
      const azureAdToken = loginResponse.idToken;

      // Step 3: Exchange Azure AD access token for internal JWT (set as HttpOnly cookie)
      const loginResponseData = await exchangeAzureToken(azureAdToken, dispatch);

      toast.success(
        `Welcome, ${loginResponseData.firstName}! You're now logged in with Azure AD.`
      );

      return true;
    } catch (err) {
      const errorMessage =
        err instanceof Error
          ? err.message
          : 'An error occurred during Azure AD login';

      console.error('Azure AD login error:', err);
      setError(errorMessage);
      toast.error(`Login failed: ${errorMessage}`);

      return false;
    } finally {
      setIsLoading(false);
    }
  };

  const logoutFromAzureAd = async () => {
    try {
      setIsLoading(true);
      // Clear the HttpOnly cookie on the server first, then sign out of Azure AD.
      await apiClient.post('/api/v1/auth/logout').catch(() => {/* ignore if already expired */});
      await instance.logoutPopup();
      dispatch(clearUser());
      toast.success('You have been logged out.');
    } catch (err) {
      console.error('Logout error:', err);
      toast.error('Failed to log out. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return {
    loginWithAzureAd,
    logoutFromAzureAd,
    isLoading,
    error,
  };
};
