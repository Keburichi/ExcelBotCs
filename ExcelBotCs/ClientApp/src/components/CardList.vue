<script setup lang="ts" generic="T extends Record<string, any>">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  items: T[]
  columns?: number
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
  const minWidth = props.columns >= 4 ? '200px' : props.columns >= 3 ? '240px' : '320px'
  return {
    gap,
    gridTemplateColumns: `repeat(auto-fill, minmax(${minWidth}, 1fr))`,
  }
})
</script>

<template>
  <div v-if="loading" class="card-list card-list--loading">
    Loading...
  </div>
  <div v-else-if="!items?.length" class="card-list card-list--empty muted">
    {{ emptyText }}
  </div>
  <div v-else class="card-list" :style="gridStyle">
    <template v-for="(item, i) in items" :key="resolveKey(item, i)">
      <slot name="item" :item="item" :index="i" />
    </template>
  </div>
</template>

<style scoped>
.card-list {
  display: grid;
  align-items: stretch;
}

.card-list--empty {
  padding: 2rem;
  text-align: center;
}
</style>
