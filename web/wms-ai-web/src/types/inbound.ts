export interface InboundNotice {
  id: string
  tenantId: string
  warehouseId: string
  noticeNumber: string
  supplierId: string
  expectedArrivalDate: string
  status: 'pending' | 'in_transit' | 'arrived' | 'completed'
  createdAt: string
}

export interface InboundNoticeItem {
  id: string
  noticeId: string
  skuCode: string
  expectedQuantity: number
  receivedQuantity: number
}

export interface InboundReceipt {
  id: string
  noticeId: string
  receiptNumber: string
  receivedAt: string
  receivedBy: string
  status: 'pending_qc' | 'qc_in_progress' | 'qc_passed' | 'qc_rejected' | 'completed'
}

export interface InboundReceiptItem {
  id: string
  receiptId: string
  skuCode: string
  receivedQuantity: number
  qcStatus: 'pending' | 'in_progress' | 'passed' | 'rejected'
}
