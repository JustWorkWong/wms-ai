import axios, { AxiosInstance } from 'axios'

export class ApiHelper {
  private client: AxiosInstance

  constructor(baseURL: string = 'http://localhost:5000') {
    this.client = axios.create({
      baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json'
      }
    })
  }

  private getHeaders(tenantId: string, warehouseId: string, userId: string = 'admin.test') {
    return {
      'X-Tenant-Id': tenantId,
      'X-Warehouse-Id': warehouseId,
      'X-User-Id': userId
    }
  }

  async createTenant(tenantId: string, name: string, warehouseId: string = 'WH_TEST_01') {
    return this.client.post('/api/platform/tenants', {
      tenantId,
      name,
      warehouseId,
      warehouseName: 'Test Warehouse',
      adminUserId: 'admin.test'
    }, {
      headers: this.getHeaders(tenantId, warehouseId)
    })
  }

  async createUser(tenantId: string, warehouseId: string, userId: string, username: string, displayName: string) {
    return this.client.post('/api/platform/users', {
      userId,
      username,
      displayName
    }, {
      headers: this.getHeaders(tenantId, warehouseId)
    })
  }

  async createInboundNotice(tenantId: string, warehouseId: string, noticeNo: string, skuCode: string = 'SKU_TEST_001', quantity: number = 100) {
    return this.client.post('/api/inbound/notices', {
      noticeNo,
      lines: [{ skuCode, quantity }]
    }, {
      headers: this.getHeaders(tenantId, warehouseId)
    })
  }

  async recordReceipt(tenantId: string, warehouseId: string, inboundNoticeId: string, skuCode: string = 'SKU_TEST_001', quantity: number = 100) {
    const receiptNo = `RCV_${Date.now()}`
    return this.client.post('/api/inbound/receipts', {
      inboundNoticeId,
      receiptNo,
      lines: [{ skuCode, quantity }]
    }, {
      headers: this.getHeaders(tenantId, warehouseId)
    })
  }

  async getQcTasks(tenantId: string, warehouseId: string) {
    return this.client.get('/api/qc/tasks', {
      headers: this.getHeaders(tenantId, warehouseId)
    })
  }

  async uploadEvidence(tenantId: string, warehouseId: string, qcTaskId: string, file: Buffer, filename: string) {
    const formData = new FormData()
    const blob = new Blob([new Uint8Array(file)], { type: 'image/jpeg' })
    formData.append('file', blob, filename)

    return this.client.post(`/api/qc/tasks/${qcTaskId}/evidence`, formData, {
      headers: {
        ...this.getHeaders(tenantId, warehouseId),
        'Content-Type': 'multipart/form-data'
      }
    })
  }

  async triggerAiInspection(tenantId: string, warehouseId: string, qcTaskId: string) {
    return this.client.post(`/api/qc/tasks/${qcTaskId}/ai-inspect`, {}, {
      headers: this.getHeaders(tenantId, warehouseId)
    })
  }

  async submitQcDecision(tenantId: string, warehouseId: string, qcTaskId: string, decision: 'accepted' | 'rejected', reason: string) {
    return this.client.post(`/api/qc/tasks/${qcTaskId}/decision`, {
      decision,
      reason
    }, {
      headers: this.getHeaders(tenantId, warehouseId)
    })
  }
}
