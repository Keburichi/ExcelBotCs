<script lang="ts" setup>
import { computed, ref, watch } from 'vue'

interface Props {
  modelValue: string // HH:mm format (24-hour)
  disabled?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

// State
const is24Hour = ref(true)
const hours = ref(12)
const minutes = ref(0)
const period = ref<'AM' | 'PM'>('PM')

// Initialize from modelValue
function parseTimeString(timeStr: string) {
  if (!timeStr)
    return

  const [h, m] = timeStr.split(':').map(Number)
  hours.value = h
  minutes.value = m

  // Set period for 12h display
  if (h >= 12) {
    period.value = 'PM'
  }
  else {
    period.value = 'AM'
  }
}

// Initialize on mount
parseTimeString(props.modelValue)

// Watch for external changes
watch(() => props.modelValue, (newValue) => {
  parseTimeString(newValue)
})

// Convert to 24-hour format for emission
function emitTime() {
  let finalHours = hours.value

  // Convert to 24h if in 12h mode
  if (!is24Hour.value) {
    if (period.value === 'PM' && finalHours !== 12) {
      finalHours += 12
    }
    else if (period.value === 'AM' && finalHours === 12) {
      finalHours = 0
    }
  }

  const timeString = `${String(finalHours).padStart(2, '0')}:${String(minutes.value).padStart(2, '0')}`
  emit('update:modelValue', timeString)
}

// Watch for changes in time values
watch([hours, minutes, period], () => {
  emitTime()
})

// Display hours (converts to 12h if needed)
const displayHours = computed({
  get: () => {
    if (is24Hour.value) {
      return hours.value
    }
    else {
      // Convert to 12h
      if (hours.value === 0)
        return 12
      if (hours.value > 12)
        return hours.value - 12
      return hours.value
    }
  },
  set: (value: number) => {
    if (is24Hour.value) {
      hours.value = Math.max(0, Math.min(23, value))
    }
    else {
      // Convert from 12h to internal 24h
      const h = Math.max(1, Math.min(12, value))
      if (period.value === 'PM' && h !== 12) {
        hours.value = h + 12
      }
      else if (period.value === 'AM' && h === 12) {
        hours.value = 0
      }
      else {
        hours.value = h
      }
    }
  },
})

// Increment/decrement functions
function incrementHours() {
  if (is24Hour.value) {
    hours.value = (hours.value + 1) % 24
  }
  else {
    displayHours.value = (displayHours.value % 12) + 1
  }
}

function decrementHours() {
  if (is24Hour.value) {
    hours.value = (hours.value - 1 + 24) % 24
  }
  else {
    displayHours.value = displayHours.value === 1 ? 12 : displayHours.value - 1
  }
}

function incrementMinutes() {
  minutes.value = (minutes.value + 5) % 60
}

function decrementMinutes() {
  minutes.value = (minutes.value - 5 + 60) % 60
}

function togglePeriod() {
  period.value = period.value === 'AM' ? 'PM' : 'AM'
  // Update hours accordingly
  if (period.value === 'PM' && hours.value < 12) {
    hours.value += 12
  }
  else if (period.value === 'AM' && hours.value >= 12) {
    hours.value -= 12
  }
}

function toggle24Hour() {
  is24Hour.value = !is24Hour.value
}

// Quick time presets
const presets = [
  { label: '9:00 AM', hours: 9, minutes: 0 },
  { label: '12:00 PM', hours: 12, minutes: 0 },
  { label: '3:00 PM', hours: 15, minutes: 0 },
  { label: '6:00 PM', hours: 18, minutes: 0 },
]

function setPreset(h: number, m: number) {
  hours.value = h
  minutes.value = m
}
</script>

<template>
  <div :class="{ disabled }" class="time-picker">
    <!-- Format Toggle -->
    <div class="format-toggle">
      <button
        :class="{ active: !is24Hour }"
        :disabled="disabled"
        class="toggle-button"
        type="button"
        @click="toggle24Hour"
      >
        12h
      </button>
      <button
        :class="{ active: is24Hour }"
        :disabled="disabled"
        class="toggle-button"
        type="button"
        @click="toggle24Hour"
      >
        24h
      </button>
    </div>

    <!-- Time Controls -->
    <div class="time-controls">
      <!-- Hours -->
      <div class="time-unit">
        <button
          :disabled="disabled"
          class="increment-button"
          title="Increase hours"
          type="button"
          @click="incrementHours"
        >
          ▲
        </button>
        <input
          v-model.number="displayHours"
          :disabled="disabled"
          :max="is24Hour ? 23 : 12"
          :min="is24Hour ? 0 : 1"
          class="time-input"
          type="number"
        >
        <button
          :disabled="disabled"
          class="decrement-button"
          title="Decrease hours"
          type="button"
          @click="decrementHours"
        >
          ▼
        </button>
      </div>

      <span class="time-separator">:</span>

      <!-- Minutes -->
      <div class="time-unit">
        <button
          :disabled="disabled"
          class="increment-button"
          title="Increase minutes by 5"
          type="button"
          @click="incrementMinutes"
        >
          ▲
        </button>
        <input
          v-model.number="minutes"
          :disabled="disabled"
          class="time-input"
          max="59"
          min="0"
          type="number"
        >
        <button
          :disabled="disabled"
          class="decrement-button"
          title="Decrease minutes by 5"
          type="button"
          @click="decrementMinutes"
        >
          ▼
        </button>
      </div>

      <!-- AM/PM Toggle (only in 12h mode) -->
      <div v-if="!is24Hour" class="period-toggle">
        <button
          :class="{ active: period === 'AM' }"
          :disabled="disabled"
          class="period-button"
          type="button"
          @click="togglePeriod"
        >
          AM
        </button>
        <button
          :class="{ active: period === 'PM' }"
          :disabled="disabled"
          class="period-button"
          type="button"
          @click="togglePeriod"
        >
          PM
        </button>
      </div>
    </div>

    <!-- Quick Presets -->
    <div class="presets">
      <span class="presets-label">Quick times:</span>
      <div class="preset-buttons">
        <button
          v-for="preset in presets"
          :key="preset.label"
          :disabled="disabled"
          class="preset-button"
          type="button"
          @click="setPreset(preset.hours, preset.minutes)"
        >
          {{ preset.label }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.time-picker {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.time-picker.disabled {
  opacity: 0.6;
  pointer-events: none;
}

/* Format Toggle */
.format-toggle {
  display: flex;
  gap: 4px;
  border-radius: 8px;
  background: var(--muted-bg);
  padding: 4px;
  width: fit-content;
}

.toggle-button {
  padding: 6px 16px;
  border: none;
  background: transparent;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--muted);
  transition: all 0.2s;
}

.toggle-button:hover {
  background: var(--border);
}

.toggle-button.active {
  background: var(--link);
  color: var(--bg);
}

/* Time Controls */
.time-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
}

.time-unit {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

.increment-button,
.decrement-button {
  width: 40px;
  height: 32px;
  border: 1px solid var(--border);
  background: var(--card);
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.75rem;
  color: var(--fg);
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.increment-button:hover,
.decrement-button:hover {
  background: var(--link);
  color: var(--bg);
  border-color: var(--link);
}

.increment-button:active,
.decrement-button:active {
  transform: scale(0.95);
}

.time-input {
  width: 60px;
  height: 48px;
  text-align: center;
  font-size: 1.5rem;
  font-weight: 600;
  border: 2px solid var(--border);
  border-radius: 8px;
  background: var(--input-bg);
  color: var(--fg);
  transition: border-color 0.2s;
}

.time-input:focus {
  outline: none;
  border-color: var(--link);
  box-shadow: 0 0 0 3px var(--ring);
}

/* Remove spinner arrows from number input */
.time-input::-webkit-inner-spin-button,
.time-input::-webkit-outer-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.time-input[type=number] {
  -moz-appearance: textfield;
}

.time-separator {
  font-size: 2rem;
  font-weight: 600;
  color: var(--fg);
  margin: 0 4px;
}

/* AM/PM Period Toggle */
.period-toggle {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-left: 8px;
}

.period-button {
  width: 50px;
  height: 36px;
  border: 1px solid var(--border);
  background: var(--card);
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--fg);
  transition: all 0.2s;
}

.period-button:hover {
  background: var(--muted-bg);
}

.period-button.active {
  background: var(--link);
  color: var(--bg);
  border-color: var(--link);
}

/* Quick Presets */
.presets {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-top: 12px;
  border-top: 1px solid var(--border);
}

.presets-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.preset-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.preset-button {
  padding: 6px 12px;
  border: 1px solid var(--border);
  background: var(--card);
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.875rem;
  color: var(--fg);
  transition: all 0.2s;
}

.preset-button:hover {
  background: var(--link);
  color: var(--bg);
  border-color: var(--link);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* Responsive */
@media (max-width: 640px) {
  .time-controls {
    flex-direction: column;
    gap: 16px;
  }

  .time-separator {
    display: none;
  }

  .period-toggle {
    flex-direction: row;
    margin-left: 0;
  }
}
</style>
