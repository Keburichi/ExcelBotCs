<script lang="ts" setup>
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue'

// Use any type for maximum flexibility - parent component handles typing
interface Props {
  modelValue: any
  options: any[]
  placeholder?: string
  formatOption?: (option: any) => string
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'Search...',
  formatOption: (option: any) => option?.Name || '',
})

const emit = defineEmits<{
  'update:modelValue': [value: any]
}>()

const searchQuery = ref('')
const isOpen = ref(false)
const dropdownRef = ref<HTMLDivElement | null>(null)
const searchInputRef = ref<HTMLInputElement | null>(null)

// Filtered options based on search query
const filteredOptions = computed(() => {
  if (!searchQuery.value.trim()) {
    return props.options
  }
  const query = searchQuery.value.toLowerCase()
  return props.options.filter(option =>
    props.formatOption(option).toLowerCase().includes(query),
  )
})

// Display value in the input
const displayValue = computed(() => {
  if (props.modelValue) {
    return props.formatOption(props.modelValue)
  }
  return ''
})

// Handle option selection
function selectOption(option: any) {
  emit('update:modelValue', option)
  searchQuery.value = ''
  isOpen.value = false
}

// Open dropdown and focus search input
async function openDropdown(event: Event) {
  event.stopPropagation()
  isOpen.value = true
  await nextTick()
  searchInputRef.value?.focus()
}

// Close dropdown when clicking outside
function handleClickOutside(event: MouseEvent) {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    isOpen.value = false
    searchQuery.value = ''
  }
}

// Clear selection
function clearSelection(event: Event) {
  event.stopPropagation()
  selectOption(null)
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div ref="dropdownRef" class="searchable-dropdown">
    <div class="input-wrapper">
      <input
        v-if="isOpen"
        ref="searchInputRef"
        v-model="searchQuery"
        :placeholder="placeholder"
        class="search-input"
        type="text"
      >
      <div
        v-else
        :class="{ 'has-value': modelValue, 'placeholder': !modelValue }"
        class="display-value"
        @click="openDropdown"
      >
        <slot v-if="modelValue" name="selected" :option="modelValue">
          {{ displayValue }}
        </slot>
        <template v-else>
          {{ placeholder }}
        </template>
      </div>
      <button
        v-if="modelValue && !isOpen"
        class="clear-button"
        title="Clear selection"
        type="button"
        @click="clearSelection"
      >
        ×
      </button>
    </div>

    <div v-if="isOpen" class="dropdown-menu">
      <div
        class="dropdown-item"
        @click="selectOption(null)"
      >
        <span class="no-selection">-- No Selection --</span>
      </div>
      <div
        v-for="option in filteredOptions"
        :key="option.Id || option.Name"
        :class="{ selected: modelValue?.Id === option.Id }"
        class="dropdown-item"
        @click="selectOption(option)"
      >
        <slot name="option" :option="option">
          {{ formatOption(option) }}
        </slot>
      </div>
      <div v-if="filteredOptions.length === 0 && searchQuery" class="dropdown-item no-results">
        No results found
      </div>
    </div>
  </div>
</template>

<style scoped>
.searchable-dropdown {
  position: relative;
  width: 100%;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.search-input,
.display-value {
  width: 100%;
  padding: 8px 32px 8px 12px;
  border: 1px solid var(--border);
  border-radius: 12px;
  font-size: 14px;
  background: var(--input-bg);
  color: var(--fg);
  outline: none;
  transition: border-color 0.2s;
}

.search-input:focus,
.display-value:focus {
  border-color: var(--link);
}

.display-value {
  cursor: pointer;
  user-select: none;
}

.display-value.placeholder {
  color: var(--muted);
}

.display-value.has-value {
  color: var(--fg);
}

.clear-button {
  position: absolute;
  right: 8px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  font-size: 24px;
  line-height: 1;
  color: var(--muted);
  cursor: pointer;
  padding: 0 4px;
  transition: color 0.2s;
}

.clear-button:hover {
  color: var(--fg);
}

.dropdown-menu {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  max-height: 300px;
  overflow-y: auto;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 12px;
  margin-top: 4px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  z-index: 1000;
}

.dropdown-item {
  padding: 10px 12px;
  cursor: pointer;
  transition: background-color 0.2s;
  color: var(--fg);
}

.dropdown-item:hover {
  background-color: var(--muted-bg);
}

.dropdown-item.selected {
  background-color: color-mix(in oklab, var(--card) 90%, var(--link) 10%);
  font-weight: 500;
}

.dropdown-item.no-results {
  color: var(--muted);
  font-style: italic;
  cursor: default;
}

.dropdown-item.no-results:hover {
  background-color: transparent;
}

.no-selection {
  color: var(--muted);
  font-style: italic;
}

/* Scrollbar styling */
.dropdown-menu::-webkit-scrollbar {
  width: 8px;
}

.dropdown-menu::-webkit-scrollbar-track {
  background: var(--muted-bg);
  border-radius: 8px;
}

.dropdown-menu::-webkit-scrollbar-thumb {
  background: var(--border);
  border-radius: 8px;
}

.dropdown-menu::-webkit-scrollbar-thumb:hover {
  background: var(--muted);
}
</style>
