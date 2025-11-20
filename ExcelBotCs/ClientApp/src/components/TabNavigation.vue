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
    <div class="flex border-b border-gray-200 dark:border-gray-700">
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
  z-index: 10;
  background-color: var(--bg, white);
}

.tab-link {
  padding: 0.75rem 1.5rem;
  font-weight: 500;
  color: rgb(107 114 128);
  border-bottom: 2px solid transparent;
  transition: all 0.2s;
  text-decoration: none;
  white-space: nowrap;
}

.tab-link:hover {
  color: rgb(59 130 246);
  border-bottom-color: rgb(59 130 246 / 0.3);
}

.tab-link.active {
  color: rgb(59 130 246);
  border-bottom-color: rgb(59 130 246);
}

.dark .tab-link {
  color: rgb(156 163 175);
}

.dark .tab-link:hover {
  color: rgb(96 165 250);
  border-bottom-color: rgb(96 165 250 / 0.3);
}

.dark .tab-link.active {
  color: rgb(96 165 250);
  border-bottom-color: rgb(96 165 250);
}
</style>
