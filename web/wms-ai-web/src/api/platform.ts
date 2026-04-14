import { apiClient } from './client'
import type { Tenant, Warehouse, User } from '@/types/platform'

export const platformApi = {
  getTenants: () =>
    apiClient.get<Tenant[]>('/api/platform/tenants'),

  getTenant: (id: string) =>
    apiClient.get<Tenant>(`/api/platform/tenants/${id}`),

  createTenant: (tenant: Partial<Tenant>) =>
    apiClient.post<Tenant>('/api/platform/tenants', tenant),

  getWarehouses: () =>
    apiClient.get<Warehouse[]>('/api/platform/warehouses'),

  getWarehouse: (id: string) =>
    apiClient.get<Warehouse>(`/api/platform/warehouses/${id}`),

  createWarehouse: (warehouse: Partial<Warehouse>) =>
    apiClient.post<Warehouse>('/api/platform/warehouses', warehouse),

  getUsers: () =>
    apiClient.get<User[]>('/api/platform/users'),

  getUser: (id: string) =>
    apiClient.get<User>(`/api/platform/users/${id}`),

  createUser: (user: Partial<User>) =>
    apiClient.post<User>('/api/platform/users', user)
}
