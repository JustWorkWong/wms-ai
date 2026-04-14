import axios from 'axios'
import { useAuthStore } from '@/stores/auth'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  timeout: 30000
})

apiClient.interceptors.request.use(config => {
  const auth = useAuthStore()
  config.headers['X-Tenant-Id'] = auth.tenantId
  config.headers['X-Warehouse-Id'] = auth.warehouseId
  config.headers['X-User-Id'] = auth.userId
  config.headers['X-Correlation-Id'] = crypto.randomUUID()
  return config
})

apiClient.interceptors.response.use(
  response => response,
  error => {
    console.error('API Error:', error)
    return Promise.reject(error)
  }
)

export { apiClient }
