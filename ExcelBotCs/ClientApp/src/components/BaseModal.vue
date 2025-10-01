<script setup lang="ts">
import { computed, onMounted, onUnmounted, watch } from 'vue';

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'update:modelValue', value: boolean): void
}>();

const props = withDefaults(defineProps<{
  modelValue: boolean
  title?: string
  description?: string
  closeOnOutsideClick?: boolean
  showCloseButton?: boolean
}>(), {
  closeOnOutsideClick: true,
  showCloseButton: true
});

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
});

function close() {
  isOpen.value = false;
  emit('close');
}

function handleOverlayClick(event: MouseEvent) {
  if (props.closeOnOutsideClick && event.target === event.currentTarget) {
    close();
  }
}

function handleEscape(event: KeyboardEvent) {
  if (event.key === 'Escape' && isOpen.value) {
    close();
  }
}

// Handle body scroll lock when modal is open
watch(isOpen, (open) => {
  if (open) {
    document.body.style.overflow = 'hidden';
  } else {
    document.body.style.overflow = '';
  }
});

// Add/remove escape key listener
onMounted(() => {
  document.addEventListener('keydown', handleEscape);
});

onUnmounted(() => {
  document.removeEventListener('keydown', handleEscape);
  document.body.style.overflow = '';
});
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="isOpen"
        class="modal-overlay"
        @click="handleOverlayClick"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="title ? 'modal-title' : undefined"
        :aria-describedby="description ? 'modal-description' : undefined"
      >
        <div class="modal-container">
          <!-- Close button -->
          <button
            v-if="showCloseButton"
            class="modal-close"
            @click="close"
            aria-label="Close modal"
            type="button"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>

          <!-- Image placeholder -->
          <div v-if="$slots.image" class="modal-image">
            <slot name="image" />
          </div>

          <!-- Header -->
          <header v-if="title || $slots.header" class="modal-header">
            <slot name="header">
              <h2 v-if="title" id="modal-title" class="modal-title">{{ title }}</h2>
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
