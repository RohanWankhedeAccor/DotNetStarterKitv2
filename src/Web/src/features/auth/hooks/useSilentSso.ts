import { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useMsal } from '@azure/msal-react';
import { selectIsAuthenticated } from '../../../lib/redux/store';
import { setLoading } from '../../../lib/redux/authSlice';
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
    // Already authenticated — nothing to do, clear loading flag.
    if (isAuthenticated) {
      dispatch(setLoading(false));
      return;
    }

    const performSilentSso = async () => {
      try {
        // If MSAL has a cached account use acquireTokenSilent (fastest path).
        // Otherwise try ssoSilent — it uses a hidden iframe to pick up an existing
        // browser/corporate session (e.g. user is already signed into Office 365).
        const tokenResponse = firstAccountId
          ? await instance.acquireTokenSilent({ scopes: apiScopes, account: accounts[0] })
          : await instance.ssoSilent({ scopes: apiScopes });

        const data = await exchangeAzureToken(tokenResponse.idToken, dispatch);
        console.log(`[SSO] Silent login successful for ${data.email}`);
      } catch (error) {
        // Expected when no Azure AD session exists — user can log in manually.
        console.log('[SSO] Silent SSO not available, manual login required');
      } finally {
        dispatch(setLoading(false));
      }
    };

    performSilentSso();
  }, [instance, firstAccountId, isAuthenticated, dispatch]);
};
