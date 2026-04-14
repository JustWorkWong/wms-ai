import { apiClient } from './client'
import type { QcTask, QcDecision, QcEvidence } from '@/types/qc'

export const qcApi = {
  getTasks: () =>
    apiClient.get<QcTask[]>('/api/inbound/qc/tasks'),

  getTask: (id: string) =>
    apiClient.get<QcTask>(`/api/inbound/qc/tasks/${id}`),

  finalizeDecision: (taskId: string, decision: QcDecision) =>
    apiClient.post(`/api/inbound/qc/tasks/${taskId}/decisions`, decision),

  uploadEvidence: (taskId: string, files: File[]) => {
    const formData = new FormData()
    files.forEach(file => formData.append('files', file))
    return apiClient.post<QcEvidence[]>(
      `/api/inbound/qc/tasks/${taskId}/evidence`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    )
  }
}
