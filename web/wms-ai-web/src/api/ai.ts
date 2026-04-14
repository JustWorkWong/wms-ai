import { apiClient } from './client'
import type { AiSession, AiMessage } from '@/types/ai'

export const aiApi = {
  createSession: (workflowType: string, contextId: string) =>
    apiClient.post<AiSession>('/api/ai/sessions', {
      workflowType,
      contextId
    }),

  getSession: (sessionId: string) =>
    apiClient.get<AiSession>(`/api/ai/sessions/${sessionId}`),

  sendMessage: (sessionId: string, content: string) =>
    apiClient.post<AiMessage>(`/api/ai/sessions/${sessionId}/messages`, {
      content
    }),

  getStreamUrl: (sessionId: string) => {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
    return `${baseUrl}/api/ai/sessions/${sessionId}/stream`
  }
}
