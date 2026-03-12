import { useState } from 'react';
import { useDispatch } from 'react-redux';
import { useMsal } from '@azure/msal-react';
import { apiScopes } from '../../../lib/msalConfig';
import { setUser } from '../../../lib/redux/authSlice';
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

      // Step 2: Use the ID token directly — it is signed for our SPA Client ID
      // and can be validated by the backend without admin consent or custom API scopes.
      const azureAdToken = loginResponse.idToken;

      // Step 3: Exchange Azure AD ID token for internal JWT
      const response = await fetch('/api/v1/auth/azure-login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          azureAdToken,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(
          errorData.detail || 'Failed to authenticate with Azure AD'
        );
      }

      // Step 4: Parse login response from backend
      const loginResponseData = await response.json();

      // Step 5: Store user info and token in Redux
      dispatch(
        setUser({
          userId: loginResponseData.userId,
          email: loginResponseData.email,
          fullName: loginResponseData.fullName,
          roles: loginResponseData.roles,
          token: loginResponseData.token,
          expiresIn: loginResponseData.expiresIn,
          authSource: 'AzureAd',
        })
      );

      toast.success(
        `Welcome, ${loginResponseData.fullName}! You're now logged in with Azure AD.`
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
      await instance.logoutPopup();
      dispatch(
        setUser({
          userId: null,
          email: null,
          fullName: null,
          roles: [],
          token: null,
          expiresIn: 0,
          authSource: null,
        })
      );
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
