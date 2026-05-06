<script setup lang="ts" generic="T extends Record<string, any>">
import type { FilterDef, FilterSelection } from '@/utils/filters.types'
import SelectMenu from '@/components/SelectMenu.vue'

const props = defineProps<{
  filters: FilterDef<T>[]
  modelValue: FilterSelection
}>()

const emit = defineEmits<{
  'update:modelValue': [FilterSelection]
}>()

function update(id: string, value: unknown | unknown[]) {
  emit('update:modelValue', { ...props.modelValue, [id]: value })
}

function asArray(v: unknown | unknown[]) {
  return Array.isArray(v) ? v : (v == null ? [] : [v])
}
</script>

<template>
  <div class="filter-bar">
    <div v-for="f in filters" :key="f.id" class="filter">
      <label class="filter-label">{{ f.label }}</label>

      <SelectMenu
        :options="f.options ?? []"
        :multiple="!!f.multiple"
        :model-value="modelValue[f.id] as any"
        :coerce="(v: unknown) => v as any"
        placeholder="All"
        @update:model-value="(v) => update(f.id, v as any)"
      />
    </div>
  </div>
</template>

<style scoped>
.filter-bar {
  display: flex;
  gap: .75rem;
  flex-wrap: wrap;
  align-items: center;
}

.filter {
  display: inline-flex;
  gap: .5rem;
  align-items: center;
}
</style>
