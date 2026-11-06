<script setup lang="ts">
// The first-batch skeleton: the same layout as a winner card, so nothing jumps when the data arrives.
withDefaults(defineProps<{ rows?: number }>(), { rows: 5 })
</script>

<template>
  <div class="history-skeleton" aria-hidden="true">
    <div v-for="row in rows" :key="row" class="skeleton-card">
      <span class="skeleton-date" />
      <div class="skeleton-body">
        <span class="skeleton-avatar" />
        <div class="skeleton-lines">
          <span class="skeleton-line skeleton-line--name" />
          <span class="skeleton-line skeleton-line--prize" />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.history-skeleton {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
}

.skeleton-card {
  display: grid;
  grid-template-columns: 5.5rem 1fr;
  gap: 1.25rem;
  align-items: center;
  padding: 1rem 1.25rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--card);
}

.skeleton-date,
.skeleton-avatar,
.skeleton-line {
  display: block;
  border-radius: 6px;
  background: color-mix(in oklab, var(--fg) 8%, var(--card));
  animation: skeleton-pulse 1.4s ease-in-out infinite;
}

.skeleton-date {
  width: 4.5rem;
  height: 1rem;
}

.skeleton-body {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.skeleton-avatar {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  flex-shrink: 0;
}

.skeleton-lines {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  flex: 1;
}

.skeleton-line--name {
  width: 30%;
  height: 0.85rem;
}

.skeleton-line--prize {
  width: 55%;
  height: 0.7rem;
}

@keyframes skeleton-pulse {
  0%,
  100% {
    opacity: 0.55;
  }

  50% {
    opacity: 1;
  }
}

/* Reduced motion comes from the global rule; no extra handling is needed here. */

@media (max-width: 600px) {
  .skeleton-card {
    grid-template-columns: 1fr;
    gap: 0.625rem;
    padding: 0.875rem 1rem;
  }
}
</style>
