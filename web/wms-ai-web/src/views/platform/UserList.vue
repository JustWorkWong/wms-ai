<template>
  <div class="user-list">
    <div class="header">
      <h1>用户列表</h1>
      <button @click="refreshUsers" class="refresh-btn">刷新</button>
    </div>

    <div v-if="loading" class="loading">加载中...</div>
    <div v-else-if="users.length === 0" class="empty">暂无数据</div>

    <table v-else class="user-table">
      <thead>
        <tr>
          <th>用户ID</th>
          <th>用户名</th>
          <th>显示名称</th>
          <th>邮箱</th>
          <th>状态</th>
          <th>创建时间</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="user in users" :key="user.id">
          <td>{{ user.id }}</td>
          <td>{{ user.username }}</td>
          <td>{{ user.displayName }}</td>
          <td>{{ user.email }}</td>
          <td>
            <span :class="['status-badge', user.status]">
              {{ user.status === 'active' ? '启用' : '停用' }}
            </span>
          </td>
          <td>{{ formatDate(user.createdAt) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { platformApi } from '@/api/platform'
import type { User } from '@/types/platform'

const users = ref<User[]>([])
const loading = ref(false)

onMounted(() => {
  fetchUsers()
})

const fetchUsers = async () => {
  loading.value = true
  try {
    const response = await platformApi.getUsers()
    users.value = response.data
  } catch (error) {
    console.error('Failed to fetch users:', error)
  } finally {
    loading.value = false
  }
}

const refreshUsers = () => {
  fetchUsers()
}

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('zh-CN')
}
</script>

<style scoped>
.user-list {
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

.user-table {
  width: 100%;
  border-collapse: collapse;
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  border-radius: 8px;
  overflow: hidden;
}

.user-table thead {
  background: #fafafa;
}

.user-table th,
.user-table td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #f0f0f0;
}

.user-table th {
  font-weight: 600;
  color: #333;
}

.user-table tbody tr:hover {
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
