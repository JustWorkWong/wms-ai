<template>
  <div class="receipt-list">
    <div class="header">
      <h1>收货单列表</h1>
      <button @click="refreshReceipts" class="refresh-btn">刷新</button>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="receipts.length === 0" class="empty">暂无数据</div>

    <table v-else class="receipt-table">
      <thead>
        <tr>
          <th>收货单号</th>
          <th>通知单ID</th>
          <th>收货时间</th>
          <th>收货人</th>
          <th>状态</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="receipt in receipts" :key="receipt.id">
          <td>{{ receipt.receiptNumber }}</td>
          <td>{{ receipt.noticeId }}</td>
          <td>{{ formatDate(receipt.receivedAt) }}</td>
          <td>{{ receipt.receivedBy }}</td>
          <td>
            <span :class="['status-badge', receipt.status]">
              {{ getStatusText(receipt.status) }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { inboundApi } from '@/api/inbound'
import type { InboundReceipt } from '@/types/inbound'

const receipts = ref<InboundReceipt[]>([])
const loading = ref(false)

onMounted(() => {
  fetchReceipts()
})

const fetchReceipts = async () => {
  loading.value = true
  try {
    const response = await inboundApi.getReceipts()
    receipts.value = response.data
  } catch (error) {
    console.error('Failed to fetch receipts:', error)
  } finally {
    loading.value = false
  }
}

const refreshReceipts = () => {
  fetchReceipts()
}

const getStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    pending_qc: '待质检',
    qc_in_progress: '质检中',
    qc_passed: '质检通过',
    qc_rejected: '质检拒绝',
    completed: '已完成'
  }
  return statusMap[status] || status
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('zh-CN')
}
</script>

<style scoped>
.receipt-list {
  padding: 2rem;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.header h1 {
  font-size: 24px;
  font-weight: 600;
  color: #333;
}

.refresh-btn {
  padding: 0.5rem 1rem;
  background: #1890ff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
}

.refresh-btn:hover {
  background: #40a9ff;
}

.loading,
.empty {
  text-align: center;
  padding: 2rem;
  color: #666;
}

.receipt-table {
  width: 100%;
  border-collapse: collapse;
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  overflow: hidden;
}

.receipt-table thead {
  background: #fafafa;
}

.receipt-table th,
.receipt-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #f0f0f0;
}

.receipt-table th {
  font-weight: 600;
  color: #333;
}

.receipt-table tbody tr:hover {
  background: #fafafa;
}

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}

.status-badge.pending_qc {
  background: #fff7e6;
  color: #fa8c16;
}

.status-badge.qc_in_progress {
  background: #e6f7ff;
  color: #1890ff;
}

.status-badge.qc_passed {
  background: #f6ffed;
  color: #52c41a;
}

.status-badge.qc_rejected {
  background: #fff1f0;
  color: #ff4d4f;
}

.status-badge.completed {
  background: #f0f0f0;
  color: #666;
}
</style>
