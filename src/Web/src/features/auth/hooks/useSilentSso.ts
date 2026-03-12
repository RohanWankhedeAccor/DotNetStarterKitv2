import { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useMsal } from '@azure/msal-react';
import { setUser } from '../../../lib/redux/authSlice';
import { selectIsAuthenticated } from '../../../lib/redux/store';
import { apiScopes } from '../../../lib/msalConfig';

/**
 * Hook for Silent Single Sign-On (SSO).
 * Automatically authenticates users on app load if they're already logged in to Azure AD.
 * Part of Phase 12: Azure AD Integration (Part 1).
 *
 * Flow:
 * 1. App loads
 * 2. Check if user is already authenticated in Redux
 * 3. If not, try to acquire token silently (no UI popup)
 * 4. If successful, exchange for internal JWT
 * 5. Store in Redux
 * 6. If silent fail, user needs to manually click "Login with Azure AD"
 *
 * This provides seamless SSO experience for users already logged into Azure AD.
 */
export const useSilentSso = () => {
  const dispatch = useDispatch();
  const { instance, accounts } = useMsal();
  const isAuthenticated = useSelector(selectIsAuthenticated);

  useEffect(() => {
    // Skip if user is already authenticated
    if (isAuthenticated) {
      return;
    }

    const performSilentSso = async () => {
      try {
        // Check if there are any accounts available
        if (accounts.length === 0) {
          console.log('[SSO] No accounts available, user must login manually');
          return;
        }

        // Get the first account (could be enhanced to handle multiple accounts)
        const account = accounts[0];

        // Step 1: Try to silently acquire token — use ID token for backend validation
        const tokenResponse = await instance.acquireTokenSilent({
          scopes: apiScopes,
          account,
        });

        const azureAdToken = tokenResponse.idToken;

        // Step 2: Exchange Azure AD token for internal JWT
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
          console.warn('[SSO] Token exchange failed:', errorData.detail);
          return;
        }

        // Step 3: Parse login response from backend
        const loginResponseData = await response.json();

        // Step 4: Store user info and token in Redux
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

        console.log(
          `[SSO] Silent login successful for ${loginResponseData.email}`
        );
      } catch (error) {
        // Silent SSO failed - this is expected if:
        // 1. No session exists in Azure AD
        // 2. Session expired
        // 3. Browser blocked popup/auth (privacy settings)
        // User will need to manually click "Login with Azure AD"
        console.log('[SSO] Silent SSO not available, manual login required', error);
      }
    };

    performSilentSso();
  }, [instance, accounts, isAuthenticated, dispatch]);
};
