<script lang="ts" setup>
import type { RecurrenceConfig } from '@/utils/ical'
import { computed, watch } from 'vue'
import DateTimePicker from '@/components/DateTimePicker.vue'
import { SignupType, signupTypeToString } from '@/features/events/events.types'
import { describeRecurrence } from '@/utils/ical'

const config = defineModel<RecurrenceConfig>({
  default: () => ({
    enabled: false,
    frequency: 'WEEKLY',
    interval: 1,
    endType: 'never',
    byWeekday: [],
  }),
})

const signupType = defineModel<SignupType>('signupType', {
  default: SignupType.SingleEvent,
})

// Signup type options
const signupTypeOptions = [
  {
    value: SignupType.LockedGroup,
    label: signupTypeToString(SignupType.LockedGroup),
    description: 'Same people participate in all occurrences',
  },
]

// Day names for weekly selection
const weekdays = [
  { value: 0, label: 'Mon', fullName: 'Monday' },
  { value: 1, label: 'Tue', fullName: 'Tuesday' },
  { value: 2, label: 'Wed', fullName: 'Wednesday' },
  { value: 3, label: 'Thu', fullName: 'Thursday' },
  { value: 4, label: 'Fri', fullName: 'Friday' },
  { value: 5, label: 'Sat', fullName: 'Saturday' },
  { value: 6, label: 'Sun', fullName: 'Sunday' },
]

// Days of month (1-31)
const monthDays = Array.from({ length: 31 }, (_, i) => i + 1)

// Computed human-readable description
const recurrenceDescription = computed(() => {
  if (!config.value.enabled)
    return ''
  return describeRecurrence(config.value)
})

// Toggle weekday selection
function toggleWeekday(day: number) {
  if (!config.value.byWeekday) {
    config.value.byWeekday = []
  }

  const index = config.value.byWeekday.indexOf(day)
  if (index === -1) {
    config.value.byWeekday.push(day)
  }
  else {
    config.value.byWeekday.splice(index, 1)
  }
}

// Check if weekday is selected
function isWeekdaySelected(day: number): boolean {
  return config.value.byWeekday?.includes(day) ?? false
}

// Watch frequency changes and reset relevant fields
watch(() => config.value.frequency, (newFreq) => {
  if (newFreq !== 'WEEKLY') {
    config.value.byWeekday = []
  }
  if (newFreq !== 'MONTHLY') {
    config.value.byMonthDay = undefined
  }
})

// Watch enabled changes
watch(() => config.value.enabled, (enabled) => {
  if (!enabled) {
    // Reset to defaults when disabled
    config.value.byWeekday = []
    config.value.byMonthDay = undefined
    config.value.endType = 'never'
    config.value.count = undefined
    config.value.until = undefined
  }
})
</script>

<script lang="ts">
// Helper function for ordinal suffix
function getOrdinalSuffix(day: number): string {
  if (day > 3 && day < 21)
    return 'th'
  switch (day % 10) {
    case 1:
      return 'st'
    case 2:
      return 'nd'
    case 3:
      return 'rd'
    default:
      return 'th'
  }
}
</script>

<template>
  <div class="recurrence-options">
    <div class="form-row">
      <label class="checkbox-label">
        <input v-model="config.enabled" type="checkbox">
        <span>Recurring Event</span>
      </label>
    </div>

    <div v-if="config.enabled" class="recurrence-config">
      <!-- Frequency Selection -->
      <div class="form-row">
        <label>Repeat</label>
        <div class="frequency-input">
          <span>Every</span>
          <input
            v-model.number="config.interval"
            class="interval-input"
            inputmode="numeric"
            max="99"
            min="1"
            pattern="[0-9]*"
            type="number"
          >
          <select v-model="config.frequency" class="frequency-select">
            <option value="DAILY">
              {{ config.interval === 1 ? 'Day' : 'Days' }}
            </option>
            <option value="WEEKLY">
              {{ config.interval === 1 ? 'Week' : 'Weeks' }}
            </option>
            <option value="MONTHLY">
              {{ config.interval === 1 ? 'Month' : 'Months' }}
            </option>
            <option value="YEARLY">
              {{ config.interval === 1 ? 'Year' : 'Years' }}
            </option>
          </select>
        </div>
      </div>

      <!-- Weekly: Day Selection -->
      <div v-if="config.frequency === 'WEEKLY'" class="form-row">
        <label>Repeat on</label>
        <div class="weekday-selector">
          <button
            v-for="day in weekdays"
            :key="day.value"
            :class="{ active: isWeekdaySelected(day.value) }"
            :title="day.fullName"
            class="weekday-button"
            type="button"
            @click="toggleWeekday(day.value)"
          >
            {{ day.label }}
          </button>
        </div>
      </div>

      <!-- Monthly: Day of Month Selection -->
      <div v-if="config.frequency === 'MONTHLY'" class="form-row">
        <label>Day of Month</label>
        <select v-model.number="config.byMonthDay" class="monthday-select">
          <option :value="undefined">
            Same day as start date
          </option>
          <option v-for="day in monthDays" :key="day" :value="day">
            {{ day }}{{ getOrdinalSuffix(day) }}
          </option>
        </select>
      </div>

      <!-- End Condition -->
      <div class="form-row">
        <label>Ends</label>
        <select v-model="config.endType" class="end-type-select">
          <option value="never">
            Never
          </option>
          <option value="count">
            After a number of occurrences
          </option>
          <option value="until">
            On a specific date
          </option>
        </select>
      </div>

      <!-- Count Input -->
      <div v-if="config.endType === 'count'" class="form-row">
        <label>Number of Occurrences</label>
        <input
          v-model.number="config.count"
          inputmode="numeric"
          max="999"
          min="1"
          pattern="[0-9]*"
          placeholder="e.g., 10"
          required
          type="number"
        >
      </div>

      <!-- Until Date Input -->
      <div v-if="config.endType === 'until'" class="form-row">
        <DateTimePicker
          v-model="config.until"
          :required="true"
          label="End Date"
        />
      </div>

      <!-- Human-readable summary -->
      <div v-if="recurrenceDescription" class="recurrence-summary">
        <strong>Summary:</strong> {{ recurrenceDescription }}
      </div>

      <!-- Signup Type Selection for Recurring Events -->
      <div class="form-row">
        <label>Signup Configuration</label>
        <div class="signup-type-options">
          <label
            v-for="option in signupTypeOptions"
            :key="option.value"
            class="signup-type-option"
          >
            <input
              v-model="signupType"
              :value="option.value"
              name="signupType"
              type="radio"
            >
            <div class="option-content">
              <span class="option-label">{{ option.label }}</span>
              <span class="option-description">{{ option.description }}</span>
            </div>
          </label>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.recurrence-options {
  width: 100%;
}

.recurrence-config {
  margin-top: 12px;
  padding: 16px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--muted-bg);
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 12px 0;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-weight: 500;
}

.checkbox-label input[type="checkbox"] {
  width: 18px;
  height: 18px;
  cursor: pointer;
  accent-color: var(--link);
  /* Improve visibility in dark themes */
  filter: brightness(1.1);
}

.frequency-input {
  display: flex;
  align-items: center;
  gap: 8px;
}

.frequency-input span {
  color: var(--fg);
  font-weight: 500;
}

.interval-input {
  width: 70px;
  padding: 8px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  color: var(--fg);
}

.frequency-select,
.end-type-select,
.monthday-select {
  flex: 1;
  padding: 8px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  color: var(--fg);
}

.weekday-selector {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.weekday-button {
  padding: 8px 12px;
  border: 2px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  color: var(--fg);
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
}

.weekday-button:hover {
  border-color: var(--link);
  background: color-mix(in oklab, var(--card) 90%, var(--link) 10%);
}

.weekday-button.active {
  border-color: var(--link);
  background: var(--link);
  color: var(--bg);
}

.recurrence-summary {
  margin-top: 16px;
  padding: 12px;
  background: var(--card);
  border-radius: 8px;
  border: 1px solid var(--border);
  font-size: 0.95rem;
  color: var(--fg);
}

.recurrence-summary strong {
  color: var(--link);
}

label {
  font-weight: 500;
  color: var(--fg);
}

input[type="number"],
input[type="text"] {
  padding: 8px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  color: var(--fg);
}

.signup-type-options {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.signup-type-option {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px;
  border: 2px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  cursor: pointer;
  transition: all 0.2s;
}

.signup-type-option:hover {
  border-color: var(--link);
  background: var(--muted-bg);
}

.signup-type-option:has(input:checked) {
  border-color: var(--link);
  border-width: 3px;
  background: var(--card);
  box-shadow: 0 0 0 2px var(--link) inset;
}

.signup-type-option:has(input:checked) .option-label {
  font-weight: 700;
}

.signup-type-option input[type="radio"] {
  margin-top: 2px;
  width: 18px;
  height: 18px;
  cursor: pointer;
  flex-shrink: 0;
  accent-color: var(--link);
  /* Improve visibility in dark themes */
  filter: brightness(1.1);
}

.option-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
  flex: 1;
}

.option-label {
  font-weight: 600;
  color: var(--fg);
}

.option-description {
  font-size: 0.875rem;
  color: var(--muted);
  font-style: italic;
}

/* Responsive */
@media (max-width: 640px) {
  .frequency-input {
    flex-wrap: wrap;
  }

  .weekday-button {
    flex: 1 1 calc(14.28% - 8px);
    min-width: 40px;
    padding: 8px 4px;
    font-size: 0.85rem;
  }
}
</style>
