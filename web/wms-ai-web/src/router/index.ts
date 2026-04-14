import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    redirect: '/qc/tasks'
  },
  {
    path: '/platform/tenants',
    name: 'TenantList',
    component: () => import('@/views/platform/TenantList.vue')
  },
  {
    path: '/platform/warehouses',
    name: 'WarehouseList',
    component: () => import('@/views/platform/WarehouseList.vue')
  },
  {
    path: '/platform/users',
    name: 'UserList',
    component: () => import('@/views/platform/UserList.vue')
  },
  {
    path: '/inbound/notices',
    name: 'NoticeList',
    component: () => import('@/views/inbound/NoticeList.vue')
  },
  {
    path: '/inbound/receipts',
    name: 'ReceiptList',
    component: () => import('@/views/inbound/ReceiptList.vue')
  },
  {
    path: '/qc/tasks',
    name: 'QcTaskList',
    component: () => import('@/views/qc/TaskList.vue')
  },
  {
    path: '/qc/tasks/:id',
    name: 'QcTaskWorkbench',
    component: () => import('@/views/qc/TaskWorkbench.vue')
  }
]

export const router = createRouter({
  history: createWebHistory(),
  routes
})
