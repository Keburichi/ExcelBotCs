<script lang="ts" setup>
import { ref } from 'vue'
import ButtonShowcaseView from '@/app/ButtonShowcaseView.vue'
import ImportView from '@/components/adminPanel/ImportView.vue'

const activeSection = ref<'imports' | 'buttons'>('imports')
</script>

<template>
  <section class="dev-resources-view">
    <h2 class="text-3xl font-bold text-gray-900 dark:text-white mb-6">
      Developer Resources
    </h2>

    <!-- Section Toggle -->
    <div class="flex gap-4 mb-6">
      <button
        :class="activeSection === 'imports'
          ? 'bg-blue-600 text-white shadow-md'
          : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'"
        class="px-4 py-2 rounded-lg font-medium transition-all"
        @click="activeSection = 'imports'"
      >
        Manual Imports
      </button>
      <button
        :class="activeSection === 'buttons'
          ? 'bg-blue-600 text-white shadow-md'
          : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'"
        class="px-4 py-2 rounded-lg font-medium transition-all"
        @click="activeSection = 'buttons'"
      >
        Button Showcase
      </button>
    </div>

    <!-- Content Sections -->
    <div v-if="activeSection === 'imports'" class="section-content">
      <ImportView />
    </div>

    <div v-if="activeSection === 'buttons'" class="section-content">
      <ButtonShowcaseView />
    </div>
  </section>
</template>

<style scoped>
.dev-resources-view {
  width: 100%;
}

.section-content {
  animation: fadeIn 0.2s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
