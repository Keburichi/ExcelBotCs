<script setup lang="ts" generic="T extends Record<string, any>">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  items: T[]
  // either a fixed column count or responsive per breakpoint (customize as you like)
  columns?: number | { sm?: number, md?: number, lg?: number }
  gap?: string | number
  itemKey?: keyof T | ((item: T, index: number) => string | number)
  emptyText?: string
  loading?: boolean
}>(), {
  columns: 3,
  gap: '1rem',
  emptyText: 'No items',
  loading: false,
})

function resolveKey(item: T, index: number) {
  return typeof props.itemKey === 'function'
    ? props.itemKey(item, index)
    : props.itemKey
      ? (item[props.itemKey] as any)
      : index
}

const gridStyle = computed(() => {
  const gap = typeof props.gap === 'number' ? `${props.gap}px` : props.gap
  if (typeof props.columns === 'number') {
    return { gap, gridTemplateColumns: `repeat(${props.columns}, minmax(0, 1fr))` }
  }
  const { sm = 1, md = 2, lg = 3 } = props.columns ?? {}
  // Simplest: apply the largest as base; optionally add utility classes or media queries for sm/md
  return { gap, gridTemplateColumns: `repeat(${lg}, minmax(0, 1fr))` }
})
</script>

<template>
  <div v-if="loading" class="card-list card-list--loading">
    Loading…
  </div>
  <div v-else-if="!items?.length" class="card-list card-list--empty">
    {{ emptyText }}
  </div>
  <div v-else class="card-list" :style="gridStyle">
    <template v-for="(item, i) in items" :key="resolveKey(item, i)">
      <slot name="item" :item="item" :index="i">
        <!-- Reasonable fallback if no slot is provided -->
        <pre class="card">{{ JSON.stringify(item, null, 2) }}</pre>
      </slot>
    </template>
  </div>
</template>

<style scoped>
.card-list {
  display: grid;
  align-items: stretch; /* Changed from 'start' to 'stretch' for uniform height */
}

.card {
  border: 1px solid rgb(var(--color-card-border));
  border-radius: 0.5rem;
  padding: 0.75rem;
  background: rgb(var(--color-card));
  height: 100%; /* Ensure card fills grid cell */
}
</style>
