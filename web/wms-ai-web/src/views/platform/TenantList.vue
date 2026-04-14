<template>
  <div class="tenant-list">
    <div class="header">
      <h1>租户列表</h1>
      <button @click="refreshTenants" class="refresh-btn">刷新</button>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="tenants.length === 0" class="empty">暂无数据</div>

    <table v-else class="tenant-table">
      <thead>
        <tr>
          <th>租户ID</th>
          <th>租户代码</th>
          <th>租户名称</th>
          <th>状态</th>
          <th>创建时间</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="tenant in tenants" :key="tenant.id">
          <td>{{ tenant.id }}</td>
          <td>{{ tenant.code }}</td>
          <td>{{ tenant.name }}</td>
          <td>
            <span :class="['status-badge', tenant.status]">
              {{ tenant.status === 'active' ? '启用' : '停用' }}
            </span>
          </td>
          <td>{{ formatDate(tenant.createdAt) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { platformApi } from '@/api/platform'
import type { Tenant } from '@/types/platform'

const tenants = ref<Tenant[]>([])
const loading = ref(false)

onMounted(() => {
  fetchTenants()
})

const fetchTenants = async () => {
  loading.value = true
  try {
    const response = await platformApi.getTenants()
    tenants.value = response.data
  } catch (error) {
    console.error('Failed to fetch tenants:', error)
  } finally {
    loading.value = false
  }
}

const refreshTenants = () => {
  fetchTenants()
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('zh-CN')
}
</script>

<style scoped>
.tenant-list {
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

.tenant-table {
  width: 100%;
  border-collapse: collapse;
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  overflow: hidden;
}

.tenant-table thead {
  background: #fafafa;
}

.tenant-table th,
.tenant-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #f0f0f0;
}

.tenant-table th {
  font-weight: 600;
  color: #333;
}

.tenant-table tbody tr:hover {
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
