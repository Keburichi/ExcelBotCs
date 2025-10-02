<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue'

const props = withDefaults(defineProps<{
  title?: string
  description?: string
  closeOnOutsideClick?: boolean
  showCloseButton?: boolean
}>(), {
  closeOnOutsideClick: true,
  showCloseButton: true,
})

const emit = defineEmits<{
  (e: 'close'): void
}>()

const isOpen = defineModel<boolean>({ required: true })

function close() {
  isOpen.value = false
  emit('close')
}

function handleOverlayClick(event: MouseEvent) {
  if (props.closeOnOutsideClick && event.target === event.currentTarget) {
    close()
  }
}

function handleEscape(event: KeyboardEvent) {
  if (event.key === 'Escape' && isOpen.value) {
    close()
  }
}

// Handle body scroll lock when modal is open
watch(isOpen, (open) => {
  if (open) {
    document.body.style.overflow = 'hidden'
  }
  else {
    document.body.style.overflow = ''
  }
})

// Add/remove escape key listener
onMounted(() => {
  document.addEventListener('keydown', handleEscape)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleEscape)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="isOpen"
        class="modal-overlay"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="title ? 'modal-title' : undefined"
        :aria-describedby="description ? 'modal-description' : undefined"
        @click="handleOverlayClick"
      >
        <div class="modal-container">
          <!-- Close button -->
          <button
            v-if="showCloseButton"
            class="modal-close"
            aria-label="Close modal"
            type="button"
            @click="close"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>

          <!-- Image placeholder -->
          <div v-if="$slots.image" class="modal-image">
            <slot name="image" />
          </div>

          <!-- Header -->
          <header v-if="title || $slots.header" class="modal-header">
            <slot name="header">
              <h2 v-if="title" id="modal-title" class="modal-title">
                {{ title }}
              </h2>
            </slot>
          </header>

          <!-- Description/Body -->
          <section class="modal-body">
            <slot name="body">
              <p v-if="description" id="modal-description" class="modal-description">
                {{ description }}
              </p>
            </slot>
            <slot />
          </section>

          <!-- Actions -->
          <footer v-if="$slots.actions" class="modal-actions">
            <slot name="actions" />
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
/* Styles are intentionally empty - all styles are in main.css for theme consistency */
</style>
