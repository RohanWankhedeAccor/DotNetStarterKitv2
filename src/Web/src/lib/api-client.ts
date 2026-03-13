import axios from 'axios'

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
      console.error('Unauthorized - please login')
    }
    return Promise.reject(error)
  }
)

export default apiClient
