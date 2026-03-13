import { useEffect, useRef } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { selectIsAuthenticated, selectUser } from '../../../lib/redux/store'
import { refreshToken } from '../../../lib/redux/authSlice'
import apiClient from '../../../lib/api-client'

const REFRESH_BUFFER_MS = 5 * 60 * 1000  // refresh when < 5 min remaining
const CHECK_INTERVAL_MS = 60 * 1000       // check every 60 s

/**
 * Proactively rotates the HttpOnly auth cookie before the JWT expires.
 *
 * Flow:
 * 1. Every 60 s, compare Date.now() against tokenExpiresAt minus a 5-min buffer.
 * 2. If within the buffer, POST /api/v1/auth/refresh (cookie sent automatically).
 * 3. Backend validates existing cookie, issues new one, returns new expiry.
 * 4. Redux tokenExpiresAt updated so the next interval check uses the fresh expiry.
 *
 * Works for both local and Azure AD authenticated users — no MSAL dependency.
 * Mounted once in AppInitializer for the lifetime of the session.
 */
export const useTokenRefresh = () => {
  const dispatch = useDispatch()
  const isAuthenticated = useSelector(selectIsAuthenticated)
  const user = useSelector(selectUser)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    if (!isAuthenticated) return

    const checkAndRefresh = async () => {
      const timeUntilExpiry = user.tokenExpiresAt - Date.now()
      if (timeUntilExpiry > REFRESH_BUFFER_MS) return

      try {
        const { data } = await apiClient.post<{ expiresIn: number; tokenExpiresAt: number }>(
          '/api/v1/auth/refresh',
        )
        dispatch(refreshToken({ expiresIn: data.expiresIn, tokenExpiresAt: data.tokenExpiresAt }))
        console.log('[TokenRefresh] Cookie rotated proactively')
      } catch (error) {
        // Refresh failed — user will get 401 on next API call and can re-login.
        console.warn('[TokenRefresh] Cookie rotation failed:', error)
      }
    }

    timerRef.current = setInterval(checkAndRefresh, CHECK_INTERVAL_MS)

    return () => {
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [isAuthenticated, user.tokenExpiresAt, dispatch])
}
