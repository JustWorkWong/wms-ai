<template>
  <div class="notice-list">
    <div class="header">
      <h1>入库通知单列表</h1>
      <button @click="refreshNotices" class="refresh-btn">刷新</button>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="notices.length === 0" class="empty">暂无数据</div>

    <table v-else class="notice-table">
      <thead>
        <tr>
          <th>通知单号</th>
          <th>供应商ID</th>
          <th>预计到货日期</th>
          <th>状态</th>
          <th>创建时间</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="notice in notices" :key="notice.id">
          <td>{{ notice.noticeNumber }}</td>
          <td>{{ notice.supplierId }}</td>
          <td>{{ formatDate(notice.expectedArrivalDate) }}</td>
          <td>
            <span :class="['status-badge', notice.status]">
              {{ getStatusText(notice.status) }}
            </span>
          </td>
          <td>{{ formatDate(notice.createdAt) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { inboundApi } from '@/api/inbound'
import type { InboundNotice } from '@/types/inbound'

const notices = ref<InboundNotice[]>([])
const loading = ref(false)

onMounted(() => {
  fetchNotices()
})

const fetchNotices = async () => {
  loading.value = true
  try {
    const response = await inboundApi.getNotices()
    notices.value = response.data
  } catch (error) {
    console.error('Failed to fetch notices:', error)
  } finally {
    loading.value = false
  }
}

const refreshNotices = () => {
  fetchNotices()
}

const getStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    pending: '待处理',
    in_transit: '在途',
    arrived: '已到货',
    completed: '已完成'
  }
  return statusMap[status] || status
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString('zh-CN')
}
</script>

<style scoped>
.notice-list {
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

.notice-table {
  width: 100%;
  border-collapse: collapse;
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  overflow: hidden;
}

.notice-table thead {
  background: #fafafa;
}

.notice-table th,
.notice-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #f0f0f0;
}

.notice-table th {
  font-weight: 600;
  color: #333;
}

.notice-table tbody tr:hover {
  background: #fafafa;
}

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}

.status-badge.pending {
  background: #fff7e6;
  color: #fa8c16;
}

.status-badge.in_transit {
  background: #e6f7ff;
  color: #1890ff;
}

.status-badge.arrived {
  background: #f6ffed;
  color: #52c41a;
}

.status-badge.completed {
  background: #f0f0f0;
  color: #666;
}
</style>
