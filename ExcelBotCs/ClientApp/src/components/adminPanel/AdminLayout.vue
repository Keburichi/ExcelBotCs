<script lang="ts" setup>
import type { Tab } from '@/components/TabNavigation.vue'
import { computed } from 'vue'
import TabNavigation from '@/components/TabNavigation.vue'
import { useAuth } from '@/composables/useAuth'

const { isDeveloper } = useAuth()

const tabs = computed<Tab[]>(() => {
  const base: Tab[] = [
    { name: 'Members', path: '/admin/members' },
    { name: 'Roles', path: '/admin/roles' },
    { name: 'Statistics', path: '/admin/statistics' },
    { name: 'Settings', path: '/admin/settings' },
  ]

  if (isDeveloper?.value) {
    base.push({ name: 'Dev Resources', path: '/admin/dev-resources' })
  }

  return base
})
</script>

<template>
  <div class="admin-layout">
    <div class="page-header">
      <h2 class="page-title">
        Admin Panel
      </h2>

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

.tab-content {
  /* Let child components handle their own backgrounds */
  width: 100%;
}

.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  color: transparent;
  letter-spacing: -0.02em;
}
</style>
