<script lang="ts" setup>
import type { FCEvent } from '@/features/events/events.types'
import { computed, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import { useEvents } from '@/composables/useEvents'
import { isRecurring, parseICalString } from '@/utils/ical'

const props = defineProps<{
  modelValue: boolean
  event: FCEvent
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'extended', event: FCEvent): void
}>()

const eventsComposable = useEvents()

// State
const occurrenceCount = ref(4)
const isSubmitting = ref(false)

// Check if event is recurring
const isRecurringEvent = computed(() => {
  return props.event.ICalString && isRecurring(props.event.ICalString)
})

// Get current occurrence count
const currentOccurrenceCount = computed(() => {
  return props.event.Occurrences?.length ?? 0
})

// Get last occurrence date
const lastOccurrenceDate = computed(() => {
  if (!props.event.Occurrences || props.event.Occurrences.length === 0)
    return null

  const lastOcc = props.event.Occurrences.reduce((latest, occ) =>
    new Date(occ.OccurrenceDate) > new Date(latest.OccurrenceDate) ? occ : latest,
  )
  return new Date(lastOcc.OccurrenceDate)
})

// Format the last occurrence date
const formattedLastDate = computed(() => {
  if (!lastOccurrenceDate.value)
    return 'No occurrences'

  return lastOccurrenceDate.value.toLocaleDateString(undefined, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
})

// Get recurrence description
const recurrenceDescription = computed(() => {
  if (!props.event.ICalString)
    return ''

  const config = parseICalString(props.event.ICalString)
  if (!config)
    return ''

  const freq = config.frequency?.toLowerCase() ?? 'unknown'
  const interval = config.interval || 1

  if (interval === 1) {
    return freq
  }
  return `every ${interval} ${freq}s`
})

// Estimate preview dates (simplified approximation)
const previewDates = computed(() => {
  if (!lastOccurrenceDate.value || !props.event.ICalString)
    return []

  const config = parseICalString(props.event.ICalString)
  if (!config)
    return []

  const dates: Date[] = []
  let currentDate = new Date(lastOccurrenceDate.value)
  const interval = config.interval || 1

  for (let i = 0; i < occurrenceCount.value; i++) {
    // Approximate next date based on frequency
    switch (config.frequency) {
      case 'DAILY':
        currentDate = new Date(currentDate.getTime() + interval * 24 * 60 * 60 * 1000)
        break
      case 'WEEKLY':
        currentDate = new Date(currentDate.getTime() + interval * 7 * 24 * 60 * 60 * 1000)
        break
      case 'MONTHLY':
        currentDate = new Date(currentDate)
        currentDate.setMonth(currentDate.getMonth() + interval)
        break
      case 'YEARLY':
        currentDate = new Date(currentDate)
        currentDate.setFullYear(currentDate.getFullYear() + interval)
        break
      default:
        currentDate = new Date(currentDate.getTime() + 7 * 24 * 60 * 60 * 1000)
    }
    dates.push(new Date(currentDate))
  }

  return dates
})

// Format preview date
function formatPreviewDate(date: Date): string {
  return date.toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

// Reset state when dialog opens
watch(() => props.modelValue, (isOpen) => {
  if (isOpen) {
    occurrenceCount.value = 4
    isSubmitting.value = false
  }
})

// Submit extension
async function extendEvent() {
  if (occurrenceCount.value < 1 || !isRecurringEvent.value) {
    return
  }

  isSubmitting.value = true

  try {
    const updatedEvent = await eventsComposable.extendEvent(props.event.Id, occurrenceCount.value)

    if (updatedEvent) {
      emit('extended', updatedEvent)
      emit('update:modelValue', false)
    }
    else {
      alert(eventsComposable.error.value || 'Failed to extend event')
    }
  }
  catch (error) {
    console.error('Error extending event:', error)
    alert('Failed to extend event. Please try again.')
  }
  finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <BaseModal
    :model-value="props.modelValue"
    :title="`Extend Event - ${event.Name}`"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <template #body>
      <div class="extend-body">
        <div v-if="!isRecurringEvent" class="error-section">
          <p>This event is not a recurring event and cannot be extended.</p>
        </div>

        <template v-else>
          <div class="event-info">
            <div class="info-row">
              <span class="info-label">Current occurrences:</span>
              <span class="info-value">{{ currentOccurrenceCount }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Last occurrence:</span>
              <span class="info-value">{{ formattedLastDate }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Recurrence pattern:</span>
              <span class="info-value recurrence">{{ recurrenceDescription }}</span>
            </div>
          </div>

          <div class="input-section">
            <label class="input-label">
              Add how many occurrences?
            </label>
            <div class="input-row">
              <input
                v-model.number="occurrenceCount"
                class="count-input"
                max="52"
                min="1"
                type="number"
              >
              <span class="input-suffix">occurrence{{ occurrenceCount === 1 ? '' : 's' }}</span>
            </div>
          </div>

          <div v-if="previewDates.length > 0" class="preview-section">
            <h4 class="preview-title">
              Preview of new occurrences:
            </h4>
            <ul class="preview-list">
              <li v-for="(date, index) in previewDates" :key="index" class="preview-item">
                + {{ formatPreviewDate(date) }}
              </li>
            </ul>
            <p class="preview-note">
              * Dates are approximate and may vary based on recurrence rules
            </p>
          </div>
        </template>
      </div>
    </template>

    <template #actions>
      <BaseButton
        state="secondary"
        title="Cancel"
        @clicked="emit('update:modelValue', false)"
      />
      <BaseButton
        v-if="isRecurringEvent"
        :disabled="isSubmitting || occurrenceCount < 1"
        :title="isSubmitting ? 'Extending...' : `Add ${occurrenceCount} Occurrence${occurrenceCount === 1 ? '' : 's'}`"
        state="primary"
        @clicked="extendEvent"
      />
    </template>
  </BaseModal>
</template>

<style scoped>
.extend-body {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.error-section {
  padding: 16px;
  background: #fee2e2;
  border: 1px solid #ef4444;
  border-radius: 8px;
  color: #dc2626;
  font-weight: 500;
}

.error-section p {
  margin: 0;
}

.event-info {
  padding: 16px;
  background: var(--muted-bg, #f9f9f9);
  border-radius: 8px;
  border: 1px solid var(--border, #e0e0e0);
}

.info-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 0;
}

.info-row:not(:last-child) {
  border-bottom: 1px solid var(--border, #e0e0e0);
}

.info-label {
  font-weight: 500;
  color: var(--muted, #666);
  font-size: 0.9rem;
}

.info-value {
  font-weight: 600;
  color: var(--fg, #333);
  font-size: 0.95rem;
}

.info-value.recurrence {
  text-transform: capitalize;
  color: var(--link, #3b82f6);
}

.input-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.input-label {
  font-weight: 600;
  color: var(--fg, #333);
  font-size: 1rem;
}

.input-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.count-input {
  width: 80px;
  padding: 10px 12px;
  font-size: 1.1rem;
  font-weight: 600;
  text-align: center;
  border: 2px solid var(--border, #e0e0e0);
  border-radius: 8px;
  background: white;
  color: var(--fg, #333);
  outline: none;
  transition: border-color 0.2s;
}

.count-input:focus {
  border-color: var(--link, #3b82f6);
}

.input-suffix {
  font-size: 1rem;
  color: var(--muted, #666);
}

.preview-section {
  padding: 16px;
  background: #ecfdf5;
  border: 1px solid #10b981;
  border-radius: 8px;
}

.preview-title {
  margin: 0 0 12px 0;
  font-size: 0.95rem;
  font-weight: 600;
  color: #047857;
}

.preview-list {
  margin: 0;
  padding: 0;
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 200px;
  overflow-y: auto;
}

.preview-item {
  font-size: 0.9rem;
  color: #047857;
  font-weight: 500;
  padding: 4px 0;
}

.preview-note {
  margin: 12px 0 0 0;
  font-size: 0.8rem;
  color: #059669;
  font-style: italic;
}
</style>
