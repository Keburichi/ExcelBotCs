<script setup lang="ts" generic="V">
import {computed, nextTick, onMounted, onUnmounted, ref} from 'vue'

const props = withDefaults(defineProps<{
  options: { label: string; value: V }[]
  modelValue: V | V | null | undefined
  multiple?: boolean
  placeholder?: string
  // Optional: when option values are numbers but come in as strings from templates
  coerce?: (v: unknown) => V
}>(), {
  multiple: false,
  placeholder: 'Select…'
})

const emit = defineEmits<{
  'update:modelValue': [V | V[] | null]
  change: [V | V[] | null]
}>()

const open = ref(false)
const buttonRef = ref<HTMLButtonElement | null>(null)
const listRef = ref<HTMLUListElement | null>(null)
const activeIndex = ref(-1)

const normalizedOptions = computed(() => props.options ?? [])

function isSelected(v: V) {
  const mv = props.modelValue as any
  return props.multiple
      ? Array.isArray(mv) && mv.some((x: any) => x === v)
      : mv === v
}

function selectAt(index: number) {
  const opt = normalizedOptions.value[index]
  if (!opt) return
  const value = props.coerce ? props.coerce(opt.value as unknown) : opt.value

  if (props.multiple) {
    const current = Array.isArray(props.modelValue) ? [...(props.modelValue as V[])] : []
    const i = current.findIndex(x => x === value)
    if (i >= 0)
      current.splice(i, 1);
    else
      current.push(value);

    emit('update:modelValue', current)
    emit('change', current)
  } else {
    emit('update:modelValue', value)
    emit('change', value)
    open.value = false
    nextTick(() => buttonRef.value?.focus())
  }
}

function onButtonKeydown(e: KeyboardEvent) {
  if (e.key === 'ArrowDown' || e.key === 'Enter' || e.key === ' ') {
    e.preventDefault()
    open.value = true
    nextTick(() => {
      activeIndex.value = Math.max(0, selectedFirstIndex.value)
      focusActive()
    })
  }
}

function onListKeydown(e: KeyboardEvent) {
  const max = normalizedOptions.value.length - 1
  if (e.key === 'Escape') {
    open.value = false;
    nextTick(() => buttonRef.value?.focus());
    return
  }
  if (e.key === 'ArrowDown') {
    e.preventDefault();
    activeIndex.value = Math.min(max, activeIndex.value + 1);
    focusActive();
    return
  }
  if (e.key === 'ArrowUp') {
    e.preventDefault();
    activeIndex.value = Math.max(0, activeIndex.value - 1);
    focusActive();
    return
  }
  if (e.key === 'Enter' || e.key === ' ') {
    e.preventDefault();
    selectAt(activeIndex.value);
    return
  }
}

function focusActive() {
  const el = listRef.value?.querySelector<HTMLElement>(`[data-index="${activeIndex.value}"]`)
  el?.focus()
}

const selectedLabels = computed(() => {
  const opts = normalizedOptions.value
  const mv = props.modelValue as any
  if (props.multiple) {
    const arr: any[] = Array.isArray(mv) ? mv : []
    const labels = arr
        .map(v => opts.find(o => o.value === v)?.label)
        .filter(Boolean)
    return labels.length ? labels.join(', ') : props.placeholder
  }
  const label = opts.find(o => o.value === mv)?.label
  return label ?? props.placeholder
})

const selectedFirstIndex = computed(() => {
  const mv = props.modelValue as any
  if (props.multiple) {
    const arr: any[] = Array.isArray(mv) ? mv : []
    const v = arr[0]
    return normalizedOptions.value.findIndex(o => o.value === v)
  }
  return normalizedOptions.value.findIndex(o => o.value === mv)
})

function onClickOutside(e: MouseEvent) {
  const t = e.target as Node
  if (!open.value) return
  if (buttonRef.value?.contains(t)) return
  if (listRef.value?.contains(t)) return
  open.value = false
}

onMounted(() => document.addEventListener('mousedown', onClickOutside))
onUnmounted(() => document.removeEventListener('mousedown', onClickOutside))
</script>

<template>
  <div class="select-menu">
    <button
        ref="buttonRef"
        class="sm-trigger"
        type="button"
        :aria-expanded="open"
        aria-haspopup="listbox"
        @click="open = !open"
        @keydown="onButtonKeydown"
    >
      <span class="sm-value">{{ selectedLabels }}</span>
      <svg class="sm-caret" viewBox="0 0 20 20" aria-hidden="true">
        <path d="M6 8l4 4 4-4" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round"/>
      </svg>
    </button>

    <ul
        v-show="open"
        ref="listRef"
        class="sm-list"
        role="listbox"
        :aria-multiselectable="multiple || undefined"
        @keydown.stop.prevent="onListKeydown"
        tabindex="-1"
    >
      <li
          v-for="(opt, i) in normalizedOptions"
          :key="String(opt.value)"
          class="sm-option"
          role="option"
          :aria-selected="isSelected(opt.value)"
          :data-index="i"
          tabindex="-1"
          @click="selectAt(i)"
      >
        <span class="sm-check" aria-hidden="true">
          <svg v-if="isSelected(opt.value)" viewBox="0 0 20 20"><path d="M5 10l3 3 7-7" stroke="currentColor"
                                                                      stroke-width="2" fill="none"
                                                                      stroke-linecap="round"/></svg>
        </span>
        <span class="sm-label">{{ opt.label }}</span>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.select-menu {
  position: relative;
  display: inline-block;
  min-width: 12rem;
}

.sm-trigger {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: .5rem;
  padding: .5rem .625rem;
  border: 1px solid #d1d5db;
  border-radius: .5rem;
  background: white;
  color: #111827;
  box-shadow: 0 1px 2px rgba(0, 0, 0, .04);
}

.sm-trigger:hover {
  border-color: #9ca3af;
}

.sm-value {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sm-caret {
  width: 1rem;
  height: 1rem;
  opacity: .7;
}

.sm-list {
  position: absolute;
  z-index: 20;
  margin-top: .25rem;
  max-height: 16rem;
  overflow: auto;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: .5rem;
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, .1), 0 4px 6px -4px rgba(0, 0, 0, .1);
  padding: .25rem;
  inset-inline: 0;
}

.sm-option {
  display: flex;
  align-items: center;
  gap: .5rem;
  padding: .375rem .5rem;
  border-radius: .375rem;
  cursor: default;
}

.sm-option:hover, .sm-option:focus {
  background: #f3f4f6;
  outline: none;
}

.sm-check {
  width: 1rem;
  height: 1rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #111827;
}

.sm-check > svg {
  width: 1rem;
  height: 1rem;
}

.sm-label {
  flex: 1 1 auto;
}
</style>