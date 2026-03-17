import axios from 'axios'
import { store } from './redux/store'
import { clearUser } from './redux/authSlice'

// Use empty base URL in dev so requests go through Vite proxy (/api → https://localhost:5001)
// This avoids browser SSL certificate warnings with self-signed dev certs
const apiBaseUrl = import.meta.env.DEV ? '' : (import.meta.env.VITE_API_BASE_URL || '')

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
  // withCredentials ensures the browser sends the HttpOnly auth_token cookie on every
  // cross-origin request and receives Set-Cookie headers from the server.
  // In dev, Vite proxies /api to the backend so all requests appear same-origin.
  withCredentials: true,
})

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Cookie has expired or is missing — clear stale Redux/localStorage auth state
      // so the UI returns to the logged-out state instead of looping on 401s.
      store.dispatch(clearUser())
    }
    return Promise.reject(error)
  }
)

export default apiClient
