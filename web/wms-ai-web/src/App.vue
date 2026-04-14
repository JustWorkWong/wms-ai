<template>
  <div id="app">
    <div class="app-layout">
      <aside class="sidebar">
        <div class="logo">
          <h2>WMS AI</h2>
        </div>
        <nav class="nav-menu">
          <div class="nav-section">
            <div class="nav-section-title">质检管理</div>
            <router-link to="/qc/tasks" class="nav-item">
              质检任务
            </router-link>
          </div>
          <div class="nav-section">
            <div class="nav-section-title">入库管理</div>
            <router-link to="/inbound/notices" class="nav-item">
              入库通知单
            </router-link>
            <router-link to="/inbound/receipts" class="nav-item">
              收货单
            </router-link>
          </div>
          <div class="nav-section">
            <div class="nav-section-title">平台管理</div>
            <router-link to="/platform/tenants" class="nav-item">
              租户管理
            </router-link>
            <router-link to="/platform/warehouses" class="nav-item">
              仓库管理
            </router-link>
            <router-link to="/platform/users" class="nav-item">
              用户管理
            </router-link>
          </div>
        </nav>
      </aside>
      <main class="main-content">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  background: #f5f5f5;
}

#app {
  height: 100vh;
}

.app-layout {
  display: flex;
  height: 100%;
}

.sidebar {
  width: 240px;
  background: #001529;
  color: white;
  display: flex;
  flex-direction: column;
}

.logo {
  padding: 1.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.logo h2 {
  font-size: 20px;
  font-weight: 600;
}

.nav-menu {
  flex: 1;
  padding: 1rem 0;
  overflow-y: auto;
}

.nav-section {
  margin-bottom: 1.5rem;
}

.nav-section-title {
  padding: 0.5rem 1.5rem;
  font-size: 12px;
  color: rgba(255, 255, 255, 0.5);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.nav-item {
  display: block;
  padding: 0.75rem 1.5rem;
  color: rgba(255, 255, 255, 0.8);
  text-decoration: none;
  transition: all 0.2s;
}

.nav-item:hover {
  background: rgba(255, 255, 255, 0.1);
  color: white;
}

.nav-item.router-link-active {
  background: #1890ff;
  color: white;
}

.main-content {
  flex: 1;
  overflow-y: auto;
  background: #f5f5f5;
}
</style>
