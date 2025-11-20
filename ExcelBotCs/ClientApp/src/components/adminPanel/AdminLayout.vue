<script lang="ts" setup>
import type { Tab } from '@/components/TabNavigation.vue'
import TabNavigation from '@/components/TabNavigation.vue'

const tabs: Tab[] = [
  { name: 'Members', path: '/admin/members' },
  { name: 'Roles', path: '/admin/roles' },
  { name: 'Statistics', path: '/admin/statistics' },
  { name: 'Settings', path: '/admin/settings' },
  { name: 'Dev Resources', path: '/admin/dev-resources' },
]
</script>

<template>
  <div class="admin-layout">
    <div class="admin-header">
      <h1 class="text-4xl font-bold text-gray-900 dark:text-white mb-6">
        Admin Panel
      </h1>

      <TabNavigation :sticky="true" :tabs="tabs" />
    </div>

    <!-- Tab Content -->
    <div class="tab-content">
      <router-view />
    </div>
  </div>
</template>

<style scoped>
.admin-layout {
  max-width: 100%;
  margin: 0 auto;
  padding: 1rem;
}

.admin-header {
  margin-bottom: 2rem;
}

.tab-content {
  /* Let child components handle their own backgrounds */
  width: 100%;
}
</style>
