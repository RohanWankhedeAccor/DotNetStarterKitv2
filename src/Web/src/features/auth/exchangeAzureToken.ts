import apiClient from '../../lib/api-client'
import type { AppDispatch } from '../../lib/redux/store'
import { setUser } from '../../lib/redux/authSlice'

interface AzureLoginResponse {
  userId: string
  email: string
  fullName: string
  roles: string[]
  token: string
  expiresIn: number
}

/**
 * Exchanges an Azure AD ID token for an internal JWT.
 * Dispatches setUser to Redux on success.
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
      fullName: data.fullName,
      roles: data.roles,
      token: data.token,
      expiresIn: data.expiresIn,
      tokenExpiresAt: Date.now() + data.expiresIn * 1000,
      authSource: 'AzureAd',
    }),
  )
  return data
}
