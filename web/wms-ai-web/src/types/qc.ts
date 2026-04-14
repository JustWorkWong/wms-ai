export interface QcTask {
  id: string
  receiptId: string
  skuCode: string
  quantity: number
  status: 'pending' | 'in_progress' | 'completed' | 'rejected'
  assignedTo?: string
  createdAt: string
  updatedAt: string
}

export interface QcDecision {
  taskId: string
  decision: 'accept' | 'reject'
  reason?: string
  evidenceUrls: string[]
  inspectedBy: string
  inspectedAt: string
}

export interface QcEvidence {
  id: string
  taskId: string
  fileUrl: string
  fileType: string
  uploadedAt: string
  uploadedBy: string
}

export interface AiSuggestion {
  taskId: string
  suggestion: 'accept' | 'reject' | 'uncertain'
  confidence: number
  reasoning: string
  evidenceAnalysis: string[]
}
