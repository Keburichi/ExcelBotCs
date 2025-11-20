<script lang="ts" setup>
import { computed, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'

// Filter controls
const selectedStates = ref<Array<'primary' | 'secondary' | 'tertiary' | 'danger'>>(['primary'])
const selectedVariants = ref<Array<'elevated' | 'outlined' | 'text'>>(['elevated'])
const selectedSizes = ref<Array<'small' | 'medium' | 'large'>>(['medium'])
const showWithIcon = ref(true)
const showWithoutIcon = ref(true)
const showDisabled = ref(false)

// All available options
const states = ['primary', 'secondary', 'tertiary', 'danger'] as const
const variants = ['elevated', 'outlined', 'text'] as const
const sizes = ['small', 'medium', 'large'] as const

// Toggle functions
function toggleState(state: typeof states[number]) {
  const index = selectedStates.value.indexOf(state)
  if (index > -1) {
    if (selectedStates.value.length > 1) {
      selectedStates.value.splice(index, 1)
    }
  }
  else {
    selectedStates.value.push(state)
  }
}

function toggleVariant(variant: typeof variants[number]) {
  const index = selectedVariants.value.indexOf(variant)
  if (index > -1) {
    if (selectedVariants.value.length > 1) {
      selectedVariants.value.splice(index, 1)
    }
  }
  else {
    selectedVariants.value.push(variant)
  }
}

function toggleSize(size: typeof sizes[number]) {
  const index = selectedSizes.value.indexOf(size)
  if (index > -1) {
    if (selectedSizes.value.length > 1) {
      selectedSizes.value.splice(index, 1)
    }
  }
  else {
    selectedSizes.value.push(size)
  }
}

// Generate button configurations
const buttonConfigurations = computed(() => {
  const configs = []

  for (const state of selectedStates.value) {
    for (const variant of selectedVariants.value) {
      for (const size of selectedSizes.value) {
        // Without icon
        if (showWithoutIcon.value) {
          configs.push({
            state,
            variant,
            size,
            title: `${state} ${variant}`,
            icon: undefined,
            iconPosition: undefined,
            disabled: false,
          })

          // Disabled variant
          if (showDisabled.value) {
            configs.push({
              state,
              variant,
              size,
              title: `${state} ${variant}`,
              icon: undefined,
              iconPosition: undefined,
              disabled: true,
            })
          }
        }

        // With icon (left)
        if (showWithIcon.value) {
          configs.push({
            state,
            variant,
            size,
            title: `${state} ${variant}`,
            icon: '🚀',
            iconPosition: 'left' as const,
            disabled: false,
          })

          // With icon (right)
          configs.push({
            state,
            variant,
            size,
            title: `${state} ${variant}`,
            icon: '🚀',
            iconPosition: 'right' as const,
            disabled: false,
          })
        }
      }
    }
  }

  return configs
})

const totalButtons = computed(() => buttonConfigurations.value.length)
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 py-8 px-4">
    <div class="max-w-7xl mx-auto">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-4xl font-bold text-gray-900 dark:text-white mb-2">
          Button Showcase
        </h1>
        <p class="text-gray-600 dark:text-gray-400">
          Interactive display of BaseButton component variations
        </p>
      </div>

      <!-- Controls Panel -->
      <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 mb-8">
        <h2 class="text-xl font-semibold text-gray-900 dark:text-white mb-4">
          Filters
          <span class="text-sm font-normal text-gray-500 ml-2">
            ({{ totalButtons }} buttons displayed)
          </span>
        </h2>

        <!-- States Filter -->
        <div class="mb-6">
          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            States
          </h3>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="state in states"
              :key="state"
              :class="
                selectedStates.includes(state)
                  ? 'bg-blue-600 text-white shadow-md'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
              "
              class="px-4 py-2 rounded-lg font-medium transition-all"
              @click="toggleState(state)"
            >
              {{ state }}
            </button>
          </div>
        </div>

        <!-- Variants Filter -->
        <div class="mb-6">
          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Variants
          </h3>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="variant in variants"
              :key="variant"
              :class="
                selectedVariants.includes(variant)
                  ? 'bg-blue-600 text-white shadow-md'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
              "
              class="px-4 py-2 rounded-lg font-medium transition-all"
              @click="toggleVariant(variant)"
            >
              {{ variant }}
            </button>
          </div>
        </div>

        <!-- Sizes Filter -->
        <div class="mb-6">
          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Sizes
          </h3>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="size in sizes"
              :key="size"
              :class="
                selectedSizes.includes(size)
                  ? 'bg-blue-600 text-white shadow-md'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
              "
              class="px-4 py-2 rounded-lg font-medium transition-all"
              @click="toggleSize(size)"
            >
              {{ size }}
            </button>
          </div>
        </div>

        <!-- Additional Options -->
        <div class="border-t border-gray-200 dark:border-gray-700 pt-4">
          <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Additional Options
          </h3>
          <div class="flex flex-wrap gap-4">
            <label class="flex items-center cursor-pointer">
              <input
                v-model="showWithIcon"
                class="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
                type="checkbox"
              >
              <span class="ml-2 text-gray-700 dark:text-gray-300">
                With Icon
              </span>
            </label>
            <label class="flex items-center cursor-pointer">
              <input
                v-model="showWithoutIcon"
                class="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
                type="checkbox"
              >
              <span class="ml-2 text-gray-700 dark:text-gray-300">
                Without Icon
              </span>
            </label>
            <label class="flex items-center cursor-pointer">
              <input
                v-model="showDisabled"
                class="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
                type="checkbox"
              >
              <span class="ml-2 text-gray-700 dark:text-gray-300">
                Show Disabled
              </span>
            </label>
          </div>
        </div>
      </div>

      <!-- Buttons Display -->
      <div class="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
        <h2 class="text-xl font-semibold text-gray-900 dark:text-white mb-6">
          Button Variations
        </h2>

        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          <div
            v-for="(config, index) in buttonConfigurations"
            :key="index"
            class="flex flex-col items-center justify-center p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
          >
            <BaseButton
              :disabled="config.disabled"
              :icon="config.icon"
              :icon-position="config.iconPosition"
              :size="config.size"
              :state="config.state"
              :title="config.title"
              :variant="config.variant"
            />
            <div class="mt-2 text-xs text-center text-gray-500 dark:text-gray-400">
              <div>{{ config.size }}</div>
              <div v-if="config.icon">
                icon: {{ config.iconPosition }}
              </div>
              <div v-if="config.disabled" class="text-red-500">
                disabled
              </div>
            </div>
          </div>
        </div>

        <div v-if="buttonConfigurations.length === 0" class="text-center py-12 text-gray-500 dark:text-gray-400">
          No buttons to display. Please select at least one option from each filter category.
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
</style>
