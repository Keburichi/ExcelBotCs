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
    <div class="tab-container">
      <router-link
        v-for="tab in tabs"
        :key="tab.name"
        :class="{ active: activeTab === tab.name }"
        :to="tab.path"
        class="tab-link"
      >
        {{ tab.name }}
      </router-link>
    </div>
  </nav>
</template>

<style scoped>
.tab-navigation {
  margin-bottom: 1.5rem;
}

.tab-navigation.sticky {
  position: sticky;
  top: 0;
  z-index: 100;
  padding: 1rem 0;
  background: rgba(255, 255, 255, 0.8);
  backdrop-filter: blur(20px);
  margin: -1rem 0 1.5rem;
  border-radius: 16px;
}

:root[data-theme='dark'] .tab-navigation.sticky {
  background: rgba(11, 16, 32, 0.8);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .tab-navigation.sticky {
    background: rgba(11, 16, 32, 0.8);
  }
}

.tab-container {
  display: flex;
  gap: 0.375rem;
  padding: 0.5rem;
  background: rgba(255, 255, 255, 0.6);
  backdrop-filter: blur(20px);
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.3);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  overflow-x: auto;
  scrollbar-width: thin;
}

:root[data-theme='dark'] .tab-container {
  background: rgba(18, 26, 45, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .tab-container {
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
  border-radius: 10px;
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
.tab-container::-webkit-scrollbar {
  height: 6px;
}

.tab-container::-webkit-scrollbar-track {
  background: transparent;
}

.tab-container::-webkit-scrollbar-thumb {
  background: rgba(var(--color-muted), 0.3);
  border-radius: 3px;
}

.tab-container::-webkit-scrollbar-thumb:hover {
  background: rgba(var(--color-muted), 0.5);
}
</style>
