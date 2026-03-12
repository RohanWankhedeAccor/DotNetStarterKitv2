import axios from 'axios'
import { store } from './redux/store'

// Use empty base URL in dev so requests go through Vite proxy (/api → https://localhost:5001)
// This avoids browser SSL certificate warnings with self-signed dev certs
const apiBaseUrl = import.meta.env.DEV ? '' : (import.meta.env.VITE_API_BASE_URL || '')

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor: attach JWT token from Redux store to every request
apiClient.interceptors.request.use((config) => {
  const token = store.getState().auth.user.token
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
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
