<template>
  <div class="warehouse-list">
    <div class="header">
      <h1>仓库列表</h1>
      <button @click="refreshWarehouses" class="refresh-btn">刷新</button>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="warehouses.length === 0" class="empty">暂无数据</div>

    <table v-else class="warehouse-table">
      <thead>
        <tr>
          <th>仓库ID</th>
          <th>租户ID</th>
          <th>仓库代码</th>
          <th>仓库名称</th>
          <th>地址</th>
          <th>状态</th>
          <th>创建时间</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="warehouse in warehouses" :key="warehouse.id">
          <td>{{ warehouse.id }}</td>
          <td>{{ warehouse.tenantId }}</td>
          <td>{{ warehouse.code }}</td>
          <td>{{ warehouse.name }}</td>
          <td>{{ warehouse.address }}</td>
          <td>
            <span :class="['status-badge', warehouse.status]">
              {{ warehouse.status === 'active' ? '启用' : '停用' }}
            </span>
          </td>
          <td>{{ formatDate(warehouse.createdAt) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { platformApi } from '@/api/platform'
import type { Warehouse } from '@/types/platform'

const warehouses = ref<Warehouse[]>([])
const loading = ref(false)

onMounted(() => {
  fetchWarehouses()
})

const fetchWarehouses = async () => {
  loading.value = true
  try {
    const response = await platformApi.getWarehouses()
    warehouses.value = response.data
  } catch (error) {
    console.error('Failed to fetch warehouses:', error)
  } finally {
    loading.value = false
  }
}

const refreshWarehouses = () => {
  fetchWarehouses()
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('zh-CN')
}
</script>

<style scoped>
.warehouse-list {
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

.warehouse-table {
  width: 100%;
  border-collapse: collapse;
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  overflow: hidden;
}

.warehouse-table thead {
  background: #fafafa;
}

.warehouse-table th,
.warehouse-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #f0f0f0;
}

.warehouse-table th {
  font-weight: 600;
  color: #333;
}

.warehouse-table tbody tr:hover {
  background: #fafafa;
}

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 500;
}

.status-badge.active {
  background: #f6ffed;
  color: #52c41a;
}

.status-badge.inactive {
  background: #f0f0f0;
  color: #999;
}
</style>
