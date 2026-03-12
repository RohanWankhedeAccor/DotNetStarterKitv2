import { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useMsal } from '@azure/msal-react';
import { selectIsAuthenticated } from '../../../lib/redux/store';
import { apiScopes } from '../../../lib/msalConfig';
import { exchangeAzureToken } from '../exchangeAzureToken';

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
  // Use homeAccountId as dependency instead of the accounts array reference.
  // MSAL recreates the array on every render even if contents are unchanged,
  // which would cause this effect to re-run and fire redundant SSO attempts.
  const firstAccountId = accounts[0]?.homeAccountId;

  useEffect(() => {
    // Skip if user is already authenticated
    if (isAuthenticated || !firstAccountId) {
      if (!firstAccountId) {
        console.log('[SSO] No accounts available, user must login manually');
      }
      return;
    }

    const performSilentSso = async () => {
      try {
        const account = accounts[0];

        // Try to silently acquire token — use ID token for backend validation
        const tokenResponse = await instance.acquireTokenSilent({
          scopes: apiScopes,
          account,
        });

        const loginResponseData = await exchangeAzureToken(tokenResponse.idToken, dispatch);
        console.log(`[SSO] Silent login successful for ${loginResponseData.email}`);
      } catch (error) {
        // Silent SSO failed — expected if no Azure AD session or session expired.
        // User will need to manually click "Login with Azure AD".
        console.log('[SSO] Silent SSO not available, manual login required', error);
      }
    };

    performSilentSso();
  }, [instance, firstAccountId, isAuthenticated, dispatch]);
};
