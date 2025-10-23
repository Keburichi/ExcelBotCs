<script setup lang="ts">
import { computed, onMounted, onUnmounted, watch } from 'vue'

const props = withDefaults(defineProps<{
  title?: string
  description?: string
  closeOnOutsideClick?: boolean
  showCloseButton?: boolean
  size?: 'small' | 'medium' | 'large'
}>(), {
  closeOnOutsideClick: true,
  showCloseButton: true,
  size: 'medium',
})

const emit = defineEmits<{
  (e: 'close'): void
}>()

const sizeClasses = computed(() => {
  const sizes = {
    small: 'modal-container-sm',
    medium: 'modal-container-md',
    large: 'modal-container-lg',
  }

  return sizes[props.size] || 'medium'
})

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
        <div class="modal-container" :class="sizeClasses">
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
.modal-container-sm{
  max-width: 500px;
}

.modal-container-md{
  max-width: 900px;
}

.modal-container-lg{
  max-width: 1300px;
}
</style>
