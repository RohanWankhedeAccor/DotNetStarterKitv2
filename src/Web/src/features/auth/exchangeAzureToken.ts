import apiClient from '../../lib/api-client'
import type { AppDispatch } from '../../lib/redux/store'
import { setUser } from '../../lib/redux/authSlice'

interface AzureLoginResponse {
  userId: string
  email: string
  firstName: string
  lastName: string
  roles: string[]
  expiresIn: number
}

/**
 * Exchanges an Azure AD access token for an internal JWT (set as HttpOnly cookie by the server).
 * Dispatches setUser to Redux with user info (not the token — it's in the cookie).
 * Shared by useAzureLogin (manual popup) and useSilentSso (auto on load).
 */
export async function exchangeAzureToken(
  azureAdToken: string,
  dispatch: AppDispatch,
): Promise<AzureLoginResponse> {
  const { data } = await apiClient.post<AzureLoginResponse>(
    '/api/v1/auth/azure-login',
    { azureAdToken },
  )
  dispatch(
    setUser({
      userId: data.userId,
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      roles: data.roles,
      expiresIn: data.expiresIn,
      tokenExpiresAt: Date.now() + data.expiresIn * 1000,
      authSource: 'AzureAd',
    }),
  )
  return data
}
