import { apiClient } from './client'
import type { InboundNotice, InboundReceipt } from '@/types/inbound'

export const inboundApi = {
  getNotices: () =>
    apiClient.get<InboundNotice[]>('/api/inbound/notices'),

  getNotice: (id: string) =>
    apiClient.get<InboundNotice>(`/api/inbound/notices/${id}`),

  createNotice: (notice: Partial<InboundNotice>) =>
    apiClient.post<InboundNotice>('/api/inbound/notices', notice),

  getReceipts: () =>
    apiClient.get<InboundReceipt[]>('/api/inbound/receipts'),

  getReceipt: (id: string) =>
    apiClient.get<InboundReceipt>(`/api/inbound/receipts/${id}`),

  createReceipt: (receipt: Partial<InboundReceipt>) =>
    apiClient.post<InboundReceipt>('/api/inbound/receipts', receipt)
}
