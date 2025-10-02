<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  state?: 'primary' | 'secondary' | 'tertiary' | 'danger' | 'pressed'
  title: string
  disabled?: boolean
  icon?: string
  iconPosition?: 'left' | 'right'
  size?: 'small' | 'medium' | 'large'
  variant?: 'elevated' | 'outlined' | 'text'
  clickable?: boolean
  tooltip?: string
}>(), {
  state: 'primary',
  disabled: false,
  iconPosition: 'left',
  variant: 'elevated',
  clickable: true,
  size: 'medium',
})

const emit = defineEmits<{ (e: 'clicked'): void }>()

// Base button classes
const baseClasses = 'inline-flex items-center justify-center font-semibold rounded-lg transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2'

// Size variants
const sizeClasses = computed(() => {
  const sizes = {
    small: 'px-3 py-1.5 text-sm gap-1.5',
    medium: 'px-4 py-2 text-base gap-2',
    large: 'px-6 py-3 text-lg gap-2.5',
  }
  return sizes[props.size || 'medium']
})

// State and variant combinations
const stateVariantClasses = computed(() => {
  const variant = props.variant || 'elevated'
  const state = props.state

  const styles = {
    elevated: {
      primary: 'bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800 shadow-md hover:shadow-lg focus:ring-blue-500',
      secondary: 'bg-gray-600 text-white hover:bg-gray-700 active:bg-gray-800 shadow-md hover:shadow-lg focus:ring-gray-500',
      tertiary: 'bg-purple-600 text-white hover:bg-purple-700 active:bg-purple-800 shadow-md hover:shadow-lg focus:ring-purple-500',
      danger: 'bg-red-600 text-white hover:bg-red-700 active:bg-red-800 shadow-md hover:shadow-lg focus:ring-red-500',
      pressed: 'bg-emerald-600 text-white hover:bg-emerald-700 active:bg-emerald-800 shadow-md hover:shadow-lg focus:ring-emerald-500',
    },
    outlined: {
      primary: 'bg-transparent text-blue-600 border-2 border-blue-600 hover:bg-blue-50 active:bg-blue-100 focus:ring-blue-500 dark:text-blue-400 dark:border-blue-400 dark:hover:bg-blue-950',
      secondary: 'bg-transparent text-gray-600 border-2 border-gray-600 hover:bg-gray-50 active:bg-gray-100 focus:ring-gray-500 dark:text-gray-400 dark:border-gray-400 dark:hover:bg-gray-950',
      tertiary: 'bg-transparent text-purple-600 border-2 border-purple-600 hover:bg-purple-50 active:bg-purple-100 focus:ring-purple-500 dark:text-purple-400 dark:border-purple-400 dark:hover:bg-purple-950',
      danger: 'bg-transparent text-red-600 border-2 border-red-600 hover:bg-red-50 active:bg-red-100 focus:ring-red-500 dark:text-red-400 dark:border-red-400 dark:hover:bg-red-950',
      pressed: 'bg-transparent text-emerald-600 border-2 border-emerald-600 hover:bg-emerald-50 active:bg-emerald-100 focus:ring-emerald-500 dark:text-emerald-400 dark:border-emerald-400 dark:hover:bg-emerald-950',
    },
    text: {
      primary: 'bg-transparent text-blue-600 hover:bg-blue-50 active:bg-blue-100 focus:ring-blue-500 dark:text-blue-400 dark:hover:bg-blue-950',
      secondary: 'bg-transparent text-gray-600 hover:bg-gray-50 active:bg-gray-100 focus:ring-gray-500 dark:text-gray-400 dark:hover:bg-gray-950',
      tertiary: 'bg-transparent text-purple-600 hover:bg-purple-50 active:bg-purple-100 focus:ring-purple-500 dark:text-purple-400 dark:hover:bg-purple-950',
      danger: 'bg-transparent text-red-600 hover:bg-red-50 active:bg-red-100 focus:ring-red-500 dark:text-red-400 dark:hover:bg-red-950',
      pressed: 'bg-transparent text-emerald-600 hover:bg-emerald-50 active:bg-emerald-100 focus:ring-emerald-500 dark:text-emerald-400 dark:hover:bg-emerald-950',
    },
  }

  return styles[variant][state]
})

// Disabled state
const disabledClasses = computed(() =>
  props.disabled ? 'opacity-50 cursor-not-allowed pointer-events-none' : 'cursor-pointer',
)

// Clickable class (if explicitly set)
const clickableClasses = computed(() =>
  props.clickable === false ? 'cursor-default' : '',
)

// Icon positioning
const iconOrderClass = computed(() =>
  props.iconPosition === 'right' ? 'flex-row-reverse' : '',
)

// Combined classes
const buttonClasses = computed(() => [
  baseClasses,
  sizeClasses.value,
  stateVariantClasses.value,
  disabledClasses.value,
  clickableClasses.value,
  iconOrderClass.value,
])
</script>

<template>
  <button
    :class="buttonClasses"
    :disabled="disabled"
    :data-tooltip="props.tooltip"
    @click="emit('clicked')"
  >
    <span v-if="props.icon" class="flex items-center">
      {{ props.icon }}
    </span>
    <span>{{ props.title }}</span>
  </button>
</template>

<style scoped>

</style>
