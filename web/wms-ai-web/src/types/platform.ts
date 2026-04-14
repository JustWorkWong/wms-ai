export interface Tenant {
  id: string
  code: string
  name: string
  status: 'active' | 'inactive'
  createdAt: string
}

export interface Warehouse {
  id: string
  tenantId: string
  code: string
  name: string
  address: string
  status: 'active' | 'inactive'
  createdAt: string
}

export interface User {
  id: string
  username: string
  displayName: string
  email: string
  status: 'active' | 'inactive'
  createdAt: string
}

export interface Membership {
  id: string
  userId: string
  tenantId: string
  warehouseId: string
  role: string
  createdAt: string
}
