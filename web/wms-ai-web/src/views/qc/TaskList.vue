<template>
  <div class="task-list">
    <div class="header">
      <h1>质检任务列表</h1>
      <button @click="refreshTasks" class="refresh-btn">刷新</button>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="tasks.length === 0" class="empty">暂无任务</div>

    <table v-else class="task-table">
      <thead>
        <tr>
          <th>任务ID</th>
          <th>收货单号</th>
          <th>SKU</th>
          <th>数量</th>
          <th>状态</th>
          <th>创建时间</th>
          <th>操作</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="task in tasks" :key="task.id">
          <td>{{ task.id }}</td>
          <td>{{ task.receiptId }}</td>
          <td>{{ task.skuCode }}</td>
          <td>{{ task.quantity }}</td>
          <td>
            <span :class="['status-badge', task.status]">
              {{ getStatusText(task.status) }}
            </span>
          </td>
          <td>{{ formatDate(task.createdAt) }}</td>
          <td>
            <router-link
              :to="`/qc/tasks/${task.id}`"
              class="action-btn"
            >
              检验
            </router-link>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useQcStore } from '@/stores/qc'
import { storeToRefs } from 'pinia'

const qcStore = useQcStore()
const { tasks, loading, error } = storeToRefs(qcStore)

onMounted(() => {
  qcStore.fetchTasks()
})

const refreshTasks = () => {
  qcStore.fetchTasks()
}

const getStatusText = (status: string) => {
  const statusMap: Record<string, string> = {
    pending: '待检验',
    in_progress: '检验中',
    completed: '已完成',
    rejected: '已拒绝'
  }
  return statusMap[status] || status
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('zh-CN')
}
</script>

<style scoped>
.task-list {
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
.error,
.empty {
  text-align: center;
  padding: 2rem;
  color: #666;
}

.error {
  color: #ff4d4f;
}

.task-table {
  width: 100%;
  border-collapse: collapse;
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  overflow: hidden;
}

.task-table thead {
  background: #fafafa;
}

.task-table th,
.task-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #f0f0f0;
}

.task-table th {
  font-weight: 600;
  color: #333;
}

.task-table tbody tr:hover {
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

.status-badge.in_progress {
  background: #e6f7ff;
  color: #1890ff;
}

.status-badge.completed {
  background: #f6ffed;
  color: #52c41a;
}

.status-badge.rejected {
  background: #fff1f0;
  color: #ff4d4f;
}

.action-btn {
  padding: 0.25rem 0.75rem;
  background: #1890ff;
  color: white;
  text-decoration: none;
  border-radius: 4px;
  font-size: 14px;
  display: inline-block;
}

.action-btn:hover {
  background: #40a9ff;
}
</style>
