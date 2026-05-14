<script lang="ts" setup>
import { ref } from 'vue'
import ButtonShowcaseView from '@/app/ButtonShowcaseView.vue'
import ImportView from '@/components/adminPanel/ImportView.vue'

const activeSection = ref<'imports' | 'buttons'>('imports')
</script>

<template>
  <section class="dev-resources-view">
    <h2 class="section-heading">
      Developer Resources
    </h2>

    <!-- Section Toggle -->
    <div class="section-toggle">
      <button
        :class="{ 'toggle-btn--active': activeSection === 'imports' }"
        class="toggle-btn"
        @click="activeSection = 'imports'"
      >
        Manual Imports
      </button>
      <button
        :class="{ 'toggle-btn--active': activeSection === 'buttons' }"
        class="toggle-btn"
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

.section-heading { font-size: 1.875rem; font-weight: 700; color: var(--fg); margin-bottom: 1.5rem; }

.section-toggle { display: flex; gap: 1rem; margin-bottom: 1.5rem; }

.toggle-btn {
  padding: 0.5rem 1rem; border-radius: 8px; font-weight: 500;
  transition: all 0.2s ease; border: none; cursor: pointer;
  background: var(--muted-bg); color: var(--fg);
}
.toggle-btn:hover { background: var(--border); }
.toggle-btn--active { background: #2563eb; color: #fff; box-shadow: 0 4px 6px -1px rgba(37,99,235,.3); }

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
