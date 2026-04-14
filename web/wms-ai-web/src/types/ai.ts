export interface AiSession {
  id: string
  workflowType: string
  contextId: string
  status: 'active' | 'completed' | 'failed'
  createdAt: string
}

export interface AiMessage {
  id: string
  sessionId: string
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: string
}

export interface AiStreamEvent {
  type: 'message' | 'status' | 'error'
  content?: string
  status?: string
  suggestion?: any
  error?: string
}
