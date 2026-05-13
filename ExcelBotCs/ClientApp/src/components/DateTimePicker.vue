<script lang="ts" setup>
import { computed, ref, watch } from 'vue'
import TimePicker from '@/components/TimePicker.vue'

interface Props {
  modelValue: Date | string
  label?: string
  required?: boolean
  minDate?: string // YYYY-MM-DD format
  disabled?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  label: 'Date & Time',
  required: false,
  disabled: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: Date]
}>()

// Separate date and time values
const dateValue = ref('')
const timeValue = ref('')

// Convert to Date object if string
function toDate(value: Date | string): Date {
  if (value instanceof Date) {
    return value
  }
  return new Date(value)
}

// Initialize from modelValue
function initializeFromDate(value: Date | string) {
  const date = toDate(value)

  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  dateValue.value = `${year}-${month}-${day}`

  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  timeValue.value = `${hours}:${minutes}`
}

// Initialize on mount
initializeFromDate(props.modelValue)

// Watch for external changes to modelValue
watch(() => props.modelValue, (newValue) => {
  initializeFromDate(newValue)
})

// Combine date and time into a Date object
function updateDateTime() {
  if (dateValue.value && timeValue.value) {
    const combinedDateTime = `${dateValue.value}T${timeValue.value}`
    const newDate = new Date(combinedDateTime)
    emit('update:modelValue', newDate)
  }
}

// Watch for changes in date or time inputs
watch([dateValue, timeValue], () => {
  updateDateTime()
})

// Computed properties for display
const selectedDateTime = computed(() => {
  if (!dateValue.value || !timeValue.value)
    return null
  return new Date(`${dateValue.value}T${timeValue.value}`)
})

const localTimeDisplay = computed(() => {
  if (!selectedDateTime.value)
    return ''
  const date = toDate(selectedDateTime.value)
  return date.toLocaleString(undefined, {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
})

const serverTimeDisplay = computed(() => {
  if (!selectedDateTime.value)
    return ''
  const date = toDate(selectedDateTime.value)
  return date.toLocaleString('en-US', {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'UTC',
  })
})

// Get timezone offset for display
const timezoneOffset = computed(() => {
  const offset = -(new Date().getTimezoneOffset() / 60)
  const sign = offset >= 0 ? '+' : '-'
  return `UTC${sign}${Math.abs(offset)}`
})
</script>

<template>
  <div class="datetime-picker">
    <label v-if="label" class="picker-label">{{ label }}</label>

    <div class="input-group">
      <div class="input-wrapper date-wrapper">
        <label class="input-label">Date</label>
        <input
          v-model="dateValue"
          :disabled="disabled"
          :min="minDate"
          :required="required"
          class="date-input"
          type="date"
        >
      </div>

      <div class="input-wrapper time-wrapper">
        <label class="input-label">Time</label>
        <TimePicker
          v-model="timeValue"
          :disabled="disabled"
        />
      </div>
    </div>

    <div v-if="selectedDateTime" class="time-preview">
      <div class="preview-row">
        <span class="preview-label">Your Timezone ({{ timezoneOffset }}):</span>
        <span class="preview-value">{{ localTimeDisplay }}</span>
      </div>
      <div class="preview-row">
        <span class="preview-label">Server Time (UTC):</span>
        <span class="preview-value">{{ serverTimeDisplay }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.datetime-picker {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.picker-label {
  font-weight: 600;
  font-size: 1rem;
  color: var(--fg);
  margin-bottom: 4px;
}

.input-group {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 24px;
  align-items: start;
}

@media (max-width: 900px) {
  .input-group {
    grid-template-columns: 1fr;
    gap: 20px;
  }
}

.input-wrapper {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.input-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.date-input {
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: 12px;
  font-size: 1rem;
  background: var(--input-bg);
  color: var(--fg);
  transition: border-color 0.2s, box-shadow 0.2s;
}

.date-input:focus {
  outline: none;
  border-color: var(--link);
  box-shadow: 0 0 0 3px var(--ring);
}

.date-input:disabled {
  background: var(--muted-bg);
  cursor: not-allowed;
  opacity: 0.6;
}

.time-preview {
  padding: 12px 16px;
  background: linear-gradient(135deg, var(--muted-bg) 0%, var(--card) 100%);
  border-radius: 8px;
  border: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.preview-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 4px 0;
}

.preview-row:not(:last-child) {
  border-bottom: 1px solid var(--border);
  padding-bottom: 8px;
}

.preview-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--muted);
}

.preview-value {
  font-size: 0.95rem;
  color: var(--fg);
  font-weight: 500;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

/* Add subtle animation when times update */
.preview-value {
  animation: fadeIn 0.3s ease-in-out;
}

@keyframes fadeIn {
  from {
    opacity: 0.5;
  }
  to {
    opacity: 1;
  }
}
</style>
