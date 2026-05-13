<script lang="ts" setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'

export interface Tab {
  name: string
  path: string
}

interface Props {
  tabs: Tab[]
  sticky?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  sticky: false,
})

const route = useRoute()

const activeTab = computed(() => {
  const currentPath = route.path
  return props.tabs.find(tab => currentPath.startsWith(tab.path))?.name || props.tabs[0]?.name
})
</script>

<template>
  <nav :class="{ sticky }" class="tab-navigation">
    <router-link
      v-for="tab in tabs"
      :key="tab.name"
      :class="{ active: activeTab === tab.name }"
      :to="tab.path"
      class="tab-link"
    >
      {{ tab.name }}
    </router-link>
  </nav>
</template>

<style scoped>
.tab-navigation {
  display: flex;
  gap: 0.375rem;
  padding: 0.5rem;
  background: rgba(255, 255, 255, 0.6);
  backdrop-filter: blur(20px);
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.3);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  overflow-x: auto;
  scrollbar-width: thin;
  margin-bottom: 1.5rem;
}

.tab-navigation.sticky {
  position: sticky;
  top: 0;
  z-index: 100;
}

:root[data-theme='dark'] .tab-navigation {
  background: rgba(18, 26, 45, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .tab-navigation {
    background: rgba(18, 26, 45, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.tab-link {
  padding: 0.625rem 1.25rem;
  font-weight: 500;
  color: var(--muted);
  border-radius: 12px;
  transition: all 0.2s ease;
  text-decoration: none;
  white-space: nowrap;
  background: transparent;
  position: relative;
}

.tab-link:hover {
  background: rgba(59, 130, 246, 0.1);
  color: var(--fg);
}

:root[data-theme='dark'] .tab-link:hover {
  background: rgba(59, 130, 246, 0.15);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .tab-link:hover {
    background: rgba(59, 130, 246, 0.15);
  }
}

.tab-link.active {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.2) 0%, rgba(147, 51, 234, 0.2) 100%);
  color: var(--fg);
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.25);
}

:root[data-theme='dark'] .tab-link.active {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.3) 0%, rgba(147, 51, 234, 0.3) 100%);
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.35);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .tab-link.active {
    background: linear-gradient(135deg, rgba(59, 130, 246, 0.3) 0%, rgba(147, 51, 234, 0.3) 100%);
    box-shadow: 0 2px 8px rgba(59, 130, 246, 0.35);
  }
}

/* Custom scrollbar for horizontal scroll */
.tab-navigation::-webkit-scrollbar {
  height: 6px;
}

.tab-navigation::-webkit-scrollbar-track {
  background: transparent;
}

.tab-navigation::-webkit-scrollbar-thumb {
  background: rgba(var(--color-muted), 0.3);
  border-radius: 8px;
}

.tab-navigation::-webkit-scrollbar-thumb:hover {
  background: rgba(var(--color-muted), 0.5);
}
</style>
