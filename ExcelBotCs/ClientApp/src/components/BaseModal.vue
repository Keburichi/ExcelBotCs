<script setup lang="ts">
import { computed, onMounted, onUnmounted, watch } from 'vue'

const props = withDefaults(defineProps<{
  title?: string
  description?: string
  closeOnOutsideClick?: boolean
  showCloseButton?: boolean
  size?: 'small' | 'medium' | 'large'
  closeGuard?: () => boolean
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
  if (props.closeGuard && !props.closeGuard()) {
    return
  }
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

/* ========================================
   Modal Component Styles
   ======================================== */

/* Modal overlay - full screen backdrop */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: var(--overlay);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

/* Modal container - the actual modal box */
.modal-container {
  position: relative;
  background: var(--card);
  border: 1px solid var(--card-border);
  border-radius: 16px;
  box-shadow: var(--elev), 0 20px 40px rgba(0, 0, 0, 0.15);
  /*max-width: 500px;*/
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  padding: 1.5rem;
}

/* Close button */
.modal-close {
  position: absolute;
  top: 1rem;
  right: 1rem;
  /*background: transparent;*/
  border: none;
  color: var(--muted);
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s, color 0.2s;
  z-index: 1;
}

.modal-close:hover {
  background: var(--muted-bg);
  color: var(--fg);
}

.modal-close:focus {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

/* Modal image slot */
.modal-image {
  margin: -1.5rem -1.5rem 1rem;
  border-radius: 16px 16px 0 0;
  overflow: hidden;
}

.modal-image img {
  width: 100%;
  height: auto;
  display: block;
}

/* Modal header */
.modal-header {
  margin-bottom: 1rem;
}

.modal-title {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--fg);
  line-height: 1.3;
  padding-right: 2rem; /* Space for close button */
}

/* Modal body */
.modal-body {
  margin-bottom: 1.5rem;
  color: var(--fg);
  line-height: 1.6;
}

.modal-description {
  margin: 0;
  color: var(--muted);
  font-size: 0.95rem;
}

/* Modal actions footer */
.modal-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  align-items: center;
  padding-top: 1rem;
  border-top: 1px solid var(--border);
}

/* Responsive adjustments */
@media (max-width: 640px) {
  .modal-container {
    max-width: 100%;
    margin: 0;
    border-radius: 12px;
  }

  .modal-actions {
    flex-direction: column-reverse;
  }

  .modal-actions > * {
    width: 100%;
  }
}

/* ========================================
   Modal Animations
   ======================================== */

/* Enter transition */
.modal-enter-active {
  transition: opacity 0.2s ease;
}

.modal-enter-active .modal-container {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.modal-enter-from {
  opacity: 0;
}

.modal-enter-from .modal-container {
  opacity: 0;
  transform: scale(0.95) translateY(-10px);
}

/* Leave transition */
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-leave-active .modal-container {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.modal-leave-to {
  opacity: 0;
}

.modal-leave-to .modal-container {
  opacity: 0;
  transform: scale(0.95) translateY(10px);
}

/* Respect prefers-reduced-motion for modal animations */
@media (prefers-reduced-motion: reduce) {
  .modal-enter-active,
  .modal-leave-active,
  .modal-enter-active .modal-container,
  .modal-leave-active .modal-container {
    transition: none !important;
  }
}
</style>
