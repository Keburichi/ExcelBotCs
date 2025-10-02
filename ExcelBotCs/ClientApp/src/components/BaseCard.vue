<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  title?: string
  subtitle?: string
  clickable?: boolean
  loading?: boolean
  variant?: 'elevated' | 'outlined' | 'flat'
  density?: 'compact' | 'comfortable'
  width?: string | number
  maxWidth?: string | number
  minHeight?: string | number
}>(), {
  clickable: false,
  loading: false,
  variant: 'elevated',
  density: 'comfortable',
})

const emit = defineEmits<{ (e: 'click'): void }>()

const variantClass = computed(() => `card--${props.variant}`)
const densityClass = computed(() => `card--${props.density}`)

const cardStyle = computed(() => ({
  width: normalize(props.width),
  maxWidth: normalize(props.maxWidth),
  minHeight: normalize(props.minHeight),
} as Record<string, string | undefined>))

function normalize(value?: string | number) {
  if (value === undefined || value === null || value === '')
    return undefined

  return typeof value === 'number' ? `${value}px` : value
}

function onClick() {
  if (props.clickable && !props.loading)
    emit('click')
}
</script>

<template>
  <article
    class="card"
    :class="[variantClass, densityClass, { clickable, loading }]"
    :tabindex="clickable ? 0 : undefined"
    role="group"
    @click="onClick"
    @keydown.enter.space.prevent="onClick"
  >
    <div v-if="$slots.image">
      <slot name="image" class="card__image" />
    </div>

    <div v-if="$slots.avatar" class="card__avatar_container">
      <slot name="avatar" />
    </div>

    <!-- Header -->
    <header v-if="$slots.header || title || subtitle" class="card__header">
      <slot name="header">
        <div class="card__titles">
          <h3 v-if="title" class="card__title">
            {{ title }}
          </h3>
          <p v-if="subtitle" class="card__subtitle">
            {{ subtitle }}
          </p>
        </div>
        <div class="card__spacer" />
        <div v-if="$slots.actions" class="card__actions">
          <slot name="actions" />
        </div>
      </slot>
    </header>

    <!-- Media (optional) -->
    <div v-if="$slots.media" class="card__media">
      <slot name="media" />
    </div>

    <!-- Default/body -->
    <section class="card__body">
      <slot name="body" />
    </section>

    <!-- Footer (optional) -->
    <footer v-if="$slots.footer" class="card__footer">
      <slot name="footer" />
    </footer>

    <!-- Loading overlay -->
    <div v-if="loading" class="card__overlay" aria-hidden="true">
      Loading…
    </div>
  </article>
</template>

<style scoped>

</style>
