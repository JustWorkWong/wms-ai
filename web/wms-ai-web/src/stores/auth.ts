import { defineStore } from 'pinia'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    tenantId: 'TENANT_DEMO',
    warehouseId: 'WH_SZ_01',
    userId: 'admin.demo',
    membershipId: null as string | null
  }),

  actions: {
    setContext(tenant: string, warehouse: string, user: string) {
      this.tenantId = tenant
      this.warehouseId = warehouse
      this.userId = user
    },

    setMembershipId(id: string) {
      this.membershipId = id
    }
  }
})
