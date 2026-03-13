import { useEffect, useRef } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { useMsal } from '@azure/msal-react'
import { selectIsAuthenticated, selectUser } from '../../../lib/redux/store'
import { apiScopes } from '../../../lib/msalConfig'
import { exchangeAzureToken } from '../exchangeAzureToken'

const REFRESH_BUFFER_MS = 5 * 60 * 1000  // refresh when < 5 min remaining
const CHECK_INTERVAL_MS = 60 * 1000       // check every 60 s

/**
 * Proactively refreshes the internal JWT before it expires.
 *
 * Flow:
 * 1. Every 60 s, compare Date.now() against tokenExpiresAt minus a 5-min buffer.
 * 2. If within the buffer, call MSAL acquireTokenSilent for a fresh Azure AD token.
 * 3. Exchange it via the backend for a fresh internal JWT.
 * 4. Redux auth state is updated — api-client interceptor picks up the new token automatically.
 *
 * Mounted once in AppInitializer for the lifetime of the session.
 */
export const useTokenRefresh = () => {
  const dispatch = useDispatch()
  const { instance, accounts } = useMsal()
  const isAuthenticated = useSelector(selectIsAuthenticated)
  const user = useSelector(selectUser)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    if (!isAuthenticated) return

    const checkAndRefresh = async () => {
      const timeUntilExpiry = user.tokenExpiresAt - Date.now()
      if (timeUntilExpiry > REFRESH_BUFFER_MS) return

      try {
        const account = accounts[0]
        if (!account) return

        const tokenResponse = await instance.acquireTokenSilent({
          scopes: apiScopes,
          account,
        })

        await exchangeAzureToken(tokenResponse.idToken, dispatch)
        console.log('[TokenRefresh] Token refreshed proactively')
      } catch (error) {
        // Silent refresh unavailable — next 401 from the API will prompt re-login.
        console.warn('[TokenRefresh] Silent refresh failed:', error)
      }
    }

    timerRef.current = setInterval(checkAndRefresh, CHECK_INTERVAL_MS)

    return () => {
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [isAuthenticated, user.tokenExpiresAt, instance, accounts, dispatch])
}
