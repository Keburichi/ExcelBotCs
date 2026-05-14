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
  <div class="showcase">
    <div class="showcase__inner">
      <!-- Header -->
      <div class="showcase__header">
        <h1 class="showcase__title">
          Button Showcase
        </h1>
        <p class="showcase__subtitle">
          Interactive display of BaseButton component variations
        </p>
      </div>

      <!-- Controls Panel -->
      <div class="showcase__panel">
        <h2 class="showcase__panel-title">
          Filters
          <span class="showcase__panel-count">
            ({{ totalButtons }} buttons displayed)
          </span>
        </h2>

        <!-- States Filter -->
        <div class="filter-section">
          <h3 class="filter-section__label">
            States
          </h3>
          <div class="filter-section__options">
            <button
              v-for="state in states"
              :key="state"
              :class="{ 'filter-btn--active': selectedStates.includes(state) }"
              class="filter-btn"
              @click="toggleState(state)"
            >
              {{ state }}
            </button>
          </div>
        </div>

        <!-- Variants Filter -->
        <div class="filter-section">
          <h3 class="filter-section__label">
            Variants
          </h3>
          <div class="filter-section__options">
            <button
              v-for="variant in variants"
              :key="variant"
              :class="{ 'filter-btn--active': selectedVariants.includes(variant) }"
              class="filter-btn"
              @click="toggleVariant(variant)"
            >
              {{ variant }}
            </button>
          </div>
        </div>

        <!-- Sizes Filter -->
        <div class="filter-section">
          <h3 class="filter-section__label">
            Sizes
          </h3>
          <div class="filter-section__options">
            <button
              v-for="size in sizes"
              :key="size"
              :class="{ 'filter-btn--active': selectedSizes.includes(size) }"
              class="filter-btn"
              @click="toggleSize(size)"
            >
              {{ size }}
            </button>
          </div>
        </div>

        <!-- Additional Options -->
        <div class="options-section">
          <h3 class="filter-section__label">
            Additional Options
          </h3>
          <div class="options-row">
            <label class="checkbox-label">
              <input
                v-model="showWithIcon"
                type="checkbox"
              >
              With Icon
            </label>
            <label class="checkbox-label">
              <input
                v-model="showWithoutIcon"
                type="checkbox"
              >
              Without Icon
            </label>
            <label class="checkbox-label">
              <input
                v-model="showDisabled"
                type="checkbox"
              >
              Show Disabled
            </label>
          </div>
        </div>
      </div>

      <!-- Buttons Display -->
      <div class="showcase__panel">
        <h2 class="showcase__panel-title" style="margin-bottom: 1.5rem;">
          Button Variations
        </h2>

        <div class="buttons-grid">
          <div
            v-for="(config, index) in buttonConfigurations"
            :key="index"
            class="button-cell"
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
            <div class="button-cell__meta">
              <div>{{ config.size }}</div>
              <div v-if="config.icon">
                icon: {{ config.iconPosition }}
              </div>
              <div v-if="config.disabled" class="button-cell__disabled">
                disabled
              </div>
            </div>
          </div>
        </div>

        <div v-if="buttonConfigurations.length === 0" class="showcase__empty">
          No buttons to display. Please select at least one option from each filter category.
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.showcase { padding: 2rem 1rem; min-height: 100vh; background: var(--bg); }
.showcase__inner { max-width: 80rem; margin: 0 auto; }
.showcase__header { margin-bottom: 2rem; }
.showcase__title { font-size: 2.25rem; font-weight: 700; color: var(--fg); margin-bottom: 0.5rem; }
.showcase__subtitle { color: var(--muted); }

.showcase__panel {
  background: var(--card); border-radius: 8px;
  box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); padding: 1.5rem; margin-bottom: 2rem;
}
.showcase__panel-title { font-size: 1.25rem; font-weight: 600; color: var(--fg); margin-bottom: 1rem; }
.showcase__panel-count { font-size: 0.875rem; font-weight: 400; color: var(--muted); margin-left: 0.5rem; }

.filter-section { margin-bottom: 1.5rem; }
.filter-section__label { font-size: 0.875rem; font-weight: 500; color: var(--muted); margin-bottom: 0.5rem; }
.filter-section__options { display: flex; flex-wrap: wrap; gap: 0.5rem; }

.filter-btn {
  padding: 0.5rem 1rem; border-radius: 8px; font-weight: 500;
  transition: all 0.2s ease; border: none; cursor: pointer;
  background: var(--muted-bg); color: var(--fg);
}
.filter-btn:hover { background: var(--border); }
.filter-btn--active { background: #2563eb; color: #fff; box-shadow: 0 4px 6px -1px rgba(37,99,235,.3); }

.options-section { border-top: 1px solid var(--border); padding-top: 1rem; }
.options-row { display: flex; flex-wrap: wrap; gap: 1rem; }
.checkbox-label {
  display: flex; align-items: center; cursor: pointer; gap: 0.5rem;
  color: var(--fg); flex-direction: row;
}

.buttons-grid {
  display: grid; grid-template-columns: repeat(1, 1fr); gap: 1rem;
}
@media (min-width: 768px) { .buttons-grid { grid-template-columns: repeat(2, 1fr); } }
@media (min-width: 1024px) { .buttons-grid { grid-template-columns: repeat(3, 1fr); } }
@media (min-width: 1280px) { .buttons-grid { grid-template-columns: repeat(4, 1fr); } }

.button-cell {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  padding: 1rem; border: 1px solid var(--border); border-radius: 8px;
  transition: background 0.15s ease;
}
.button-cell:hover { background: var(--muted-bg); }
.button-cell__meta { margin-top: 0.5rem; font-size: 0.75rem; text-align: center; color: var(--muted); }
.button-cell__disabled { color: var(--danger); }

.showcase__empty { text-align: center; padding: 3rem 0; color: var(--muted); }
</style>
