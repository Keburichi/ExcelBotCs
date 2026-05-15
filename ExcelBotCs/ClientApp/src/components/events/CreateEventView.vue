<script lang="ts" setup>
import type { FCEvent } from '@/features/events/events.types'
import type { Fight } from '@/features/fights/fights.types'
import type { RecurrenceConfig } from '@/utils/ical'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import DateTimePicker from '@/components/DateTimePicker.vue'
import RecurrenceOptions from '@/components/events/RecurrenceOptions.vue'
import SearchableDropdown from '@/components/SearchableDropdown.vue'
import { useAuth } from '@/composables/useAuth'
import { EventsApi } from '@/features/events/events.api'
import { EventType, SignupType } from '@/features/events/events.types'
import { FightsApi } from '@/features/fights/fights.api'
import { fightTypeToString } from '@/features/fights/fights.types'
import mapsPlaceholder from '@/static/img/maps-placeholder.png'
import { generateICalString, parseICalString } from '@/utils/ical'

const router = useRouter()
const route = useRoute()
const { user, isAdmin, loadMe } = useAuth()

// Ensures that provided URL is absolute (includes protocol and host)
function toAbsoluteUrl(url: string): string {
  if (!url)
    return url
  // Already absolute (supports protocol-relative //host/... too)
  if (/^(?:[a-z]+:)?\/\//i.test(url))
    return url
  // Ensure leading slash for relative asset paths
  const withSlash = url.startsWith('/') ? url : `/${url}`
  return `${window.location.origin}${withSlash}`
}

// Absolute URL to the maps placeholder image
const mapsPlaceholderAbsolute = toAbsoluteUrl(mapsPlaceholder)

const loading = ref(false)
const error = ref('')
const isEditMode = computed(() => !!route.params.id)

// Fights for dropdown
const fights = ref<Fight[]>([])
const selectedFight = ref<Fight | null>(null)

// Party size presets
type PartyPreset = 'light-party' | 'full-party' | 'alliance-raid' | 'any' | 'custom'
const partyPreset = ref<PartyPreset>('full-party')

const form = reactive<FCEvent>({
  Id: '',
  Name: '',
  Description: '',
  DiscordMessageId: '',
  PictureUrl: '',
  Type: EventType.Other,
  FightId: undefined,
  Organizer: '', // will be filled from current user on submit, server sets Author,
  StartDate: new Date(),
  EndDate: new Date(),
  // Default duration set to 30 minutes
  Duration: 30,
  ICalString: '',
  MaxNumberOfParticipants: 8,
  Signups: [],
  Groups: [],
  AvailableForSignup: false,
})

// Recurrence configuration
const recurrence = ref<RecurrenceConfig>({
  enabled: false,
  frequency: 'WEEKLY',
  interval: 1,
  endType: 'never',
  byWeekday: [],
})

// Signup type configuration
const signupType = ref<SignupType>(SignupType.SingleEvent)

// Watch recurrence.enabled and set appropriate default signup type
watch(() => recurrence.value.enabled, (enabled) => {
  if (enabled) {
    // When enabling recurrence, default to LockedGroup
    if (signupType.value === SignupType.SingleEvent) {
      signupType.value = SignupType.LockedGroup
    }
  }
  else {
    // When disabling recurrence, always set to SingleEvent
    signupType.value = SignupType.SingleEvent
  }
})

// Function to set party preset and update max participants
function setPartyPreset(preset: PartyPreset) {
  partyPreset.value = preset

  switch (preset) {
    case 'light-party':
      form.MaxNumberOfParticipants = 4
      break
    case 'full-party':
      form.MaxNumberOfParticipants = 8
      break
    case 'alliance-raid':
      form.MaxNumberOfParticipants = 24
      break
    case 'any':
      form.MaxNumberOfParticipants = 99
      break
    case 'custom':
      // Keep current value or set to 0 if invalid
      if (form.MaxNumberOfParticipants === 4 || form.MaxNumberOfParticipants === 8
        || form.MaxNumberOfParticipants === 24 || form.MaxNumberOfParticipants === 99) {
        form.MaxNumberOfParticipants = 0
      }
      break
  }
}

// Quick setters for common durations (in minutes)
function setDuration(minutes: number) {
  form.Duration = minutes
}

// Determine preset from MaxNumberOfParticipants value
function detectPreset(maxParticipants: number): PartyPreset {
  switch (maxParticipants) {
    case 4:
      return 'light-party'
    case 8:
      return 'full-party'
    case 24:
      return 'alliance-raid'
    case 99:
      return 'any'
    default:
      return 'custom'
  }
}

// Check if input should be disabled (all presets except custom)
const isInputDisabled = computed(() => partyPreset.value !== 'custom')

// Event type options for dropdown
const eventTypeOptions = computed(() => {
  return Object.keys(EventType)
    .filter(key => Number.isNaN(Number(key)))
    .map(key => ({
      value: EventType[key as keyof typeof EventType],
      label: key,
    }))
})

// Determine if fight selection should be shown based on event type
const showFightSelection = computed(() => {
  const fightCompatibleTypes = [
    EventType.Academy,
    EventType.Downsynced,
    EventType.BLU,
    EventType.Farming,
    EventType.Raid,
    EventType.MinIlvl,
    EventType.Other,
  ]
  return fightCompatibleTypes.includes(form.Type)
})

// Format fight for display in dropdown
function formatFight(fight: Fight): string {
  return `${fight.Name} (${fightTypeToString(fight.Type)})`
}

// Load fights and event data if in edit mode
onMounted(async () => {
  // Load fights for dropdown
  try {
    fights.value = await FightsApi.list()
  }
  catch (e: any) {
    console.error('Failed to load fights:', e)
  }

  if (isEditMode.value) {
    loading.value = true
    try {
      const eventData = await EventsApi.get(route.params.id as string)
      if (eventData) {
        Object.assign(form, eventData)
        // Normalize picture URL to absolute if present
        if (form.PictureUrl) {
          form.PictureUrl = toAbsoluteUrl(form.PictureUrl)
        }
        // Detect and set the appropriate preset
        partyPreset.value = detectPreset(eventData.MaxNumberOfParticipants)
        // Set selected fight if FightId exists
        if (eventData.FightId) {
          selectedFight.value = fights.value.find(f => f.Id === eventData.FightId) || null
        }
        // Parse recurrence configuration from iCal string
        if (eventData.ICalString) {
          const parsedRecurrence = parseICalString(eventData.ICalString)
          if (parsedRecurrence) {
            recurrence.value = parsedRecurrence
          }
        }
        // Set signup type from event data
        if (eventData.SignupType !== undefined) {
          signupType.value = eventData.SignupType
        }
      }
    }
    catch (e: any) {
      error.value = e?.message || 'Failed to load event'
    }
    finally {
      loading.value = false
    }
  }
})

// Watch fight selection and auto-populate image
watch(selectedFight, (newFight) => {
  if (newFight) {
    if (newFight.ImageUrl) {
      form.PictureUrl = toAbsoluteUrl(newFight.ImageUrl)
    }
    form.FightId = newFight.Id
  }
  else {
    form.FightId = undefined
  }
})

// When Maps is selected as event type, auto-set the picture to the maps placeholder
watch(
  () => form.Type,
  (newType) => {
    if (newType === EventType.Maps) {
      // Only auto-fill if not already set to a custom value
      if (!form.PictureUrl || form.PictureUrl === mapsPlaceholder || form.PictureUrl === mapsPlaceholderAbsolute) {
        form.PictureUrl = mapsPlaceholderAbsolute
      }
    }
    else {
      // If leaving Maps and the picture is the placeholder (and no fight image overrides it), clear it
      if ((form.PictureUrl === mapsPlaceholder || form.PictureUrl === mapsPlaceholderAbsolute) && !selectedFight.value) {
        form.PictureUrl = ''
      }
    }

    // Clear fight selection if switching to a type that doesn't support fights
    if (!showFightSelection.value) {
      selectedFight.value = null
      form.FightId = undefined
    }
  },
  { immediate: true },
)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await loadMe()
    if (!isAdmin.value) {
      error.value = 'You do not have permission to create/edit events.'
      return
    }

    // Organizer is computed from Author on the backend; we can set it for display
    form.Organizer = user.value?.PlayerName ?? ''

    // Ensure picture URL is absolute before sending to backend
    if (form.PictureUrl) {
      form.PictureUrl = toAbsoluteUrl(form.PictureUrl)
    }

    // Set signup type
    form.SignupType = signupType.value

    // Generate iCal string with recurrence configuration
    form.ICalString = generateICalString(form, recurrence.value)

    if (isEditMode.value) {
      await EventsApi.update(form.Id, form)
    }
    else {
      await EventsApi.create(form)
    }

    await router.push({ name: 'events' })
  }
  catch (e: any) {
    error.value = e?.message || `Failed to ${isEditMode.value ? 'update' : 'create'} event`
  }
  finally {
    loading.value = false
  }
}

function cancel() {
  router.push({ name: 'events' })
}
</script>

<template>
  <section class="page create-event">
    <div class="page-header">
      <h2 class="page-title">
        {{ isEditMode ? 'Edit FC Event' : 'Create FC Event' }}
      </h2>
    </div>
    <p v-if="error" class="error">
      {{ error }}
    </p>
    <form class="event-form" @submit.prevent="submit">
      <!-- Basic Information -->
      <section class="form-section info-section">
        <h3 class="section-header">
          Basic Information
        </h3>
        <div class="form-row">
          <label>Name</label>
          <input v-model="form.Name" placeholder="Event name" required type="text">
        </div>
        <div class="form-row">
          <label>Description</label>
          <textarea v-model="form.Description" placeholder="Describe the event" rows="5" />
        </div>
        <div class="form-row-group">
          <div class="form-row">
            <label>Event Type</label>
            <select v-model.number="form.Type" required>
              <option v-for="option in eventTypeOptions" :key="option.value" :value="option.value">
                {{ option.label }}
              </option>
            </select>
          </div>
          <div v-if="form.DiscordMessageId" class="form-row">
            <label>Discord Message Id</label>
            <input v-model="form.DiscordMessageId" placeholder="The message id of the discord post." type="text">
          </div>
        </div>
      </section>

      <!-- Media -->
      <section class="form-section media-section">
        <h3 class="section-header">
          {{ showFightSelection ? 'Fight & Media' : 'Media' }}
        </h3>
        <div v-if="showFightSelection" class="form-row">
          <label>Select Fight (Optional)</label>
          <SearchableDropdown
            v-model="selectedFight"
            :format-option="formatFight"
            :options="fights"
            placeholder="Search fights..."
          />
          <small class="hint">Selecting a fight will auto-fill the picture URL</small>
        </div>
        <div class="media-row">
          <div class="form-row media-input">
            <label>Picture URL (optional)</label>
            <input
              v-model="form.PictureUrl"
              :placeholder="showFightSelection ? 'https://... (auto-filled from fight if selected)' : 'https://...'"
              type="url"
            >
          </div>
          <div v-if="form.PictureUrl" class="image-preview">
            <img
              :src="form.PictureUrl" alt="Event preview"
              @error="(e) => (e.target as HTMLImageElement).style.display = 'none'"
            >
          </div>
        </div>
      </section>

      <!-- Schedule -->
      <section class="form-section schedule-section">
        <h3 class="section-header">
          Schedule
        </h3>
        <div class="form-row">
          <DateTimePicker
            v-model="form.StartDate"
            :required="true"
            label="Start Date & Time"
          />
        </div>
        <div class="form-row">
          <label>Duration (minutes)</label>
          <div class="duration-controls">
            <div class="duration-presets">
              <BaseButton
                :state="form.Duration === 60 ? 'primary' : 'secondary'"
                :variant="form.Duration === 60 ? 'elevated' : 'outlined'"
                size="small"
                title="60 min"
                tooltip="Set duration to 60 minutes"
                type="button"
                @clicked="setDuration(60)"
              />
              <BaseButton
                :state="form.Duration === 120 ? 'primary' : 'secondary'"
                :variant="form.Duration === 120 ? 'elevated' : 'outlined'"
                size="small"
                title="120 min"
                tooltip="Set duration to 120 minutes"
                type="button"
                @clicked="setDuration(120)"
              />
              <BaseButton
                :state="form.Duration === 180 ? 'primary' : 'secondary'"
                :variant="form.Duration === 180 ? 'elevated' : 'outlined'"
                size="small"
                title="180 min"
                tooltip="Set duration to 180 minutes"
                type="button"
                @clicked="setDuration(180)"
              />
            </div>
            <input
              v-model.number="form.Duration" class="duration-input" inputmode="numeric" min="0"
              pattern="[0-9]*" placeholder="Custom" required type="number"
            >
          </div>
        </div>
        <div class="form-row">
          <RecurrenceOptions v-model="recurrence" v-model:signup-type="signupType" />
        </div>
      </section>

      <!-- Participants -->
      <section class="form-section participants-section">
        <h3 class="section-header">
          Participants
        </h3>
        <div class="form-row">
          <label>Max Number of Participants</label>
          <div class="party-preset-buttons">
            <BaseButton
              :state="partyPreset === 'light-party' ? 'primary' : 'secondary'"
              :variant="partyPreset === 'light-party' ? 'elevated' : 'outlined'"
              size="small"
              title="Light Party (4)"
              type="button"
              @clicked="setPartyPreset('light-party')"
            />
            <BaseButton
              :state="partyPreset === 'full-party' ? 'primary' : 'secondary'"
              :variant="partyPreset === 'full-party' ? 'elevated' : 'outlined'"
              size="small"
              title="Full Party (8)"
              type="button"
              @clicked="setPartyPreset('full-party')"
            />
            <BaseButton
              :state="partyPreset === 'alliance-raid' ? 'primary' : 'secondary'"
              :variant="partyPreset === 'alliance-raid' ? 'elevated' : 'outlined'"
              size="small"
              title="Alliance Raid (24)"
              type="button"
              @clicked="setPartyPreset('alliance-raid')"
            />
            <BaseButton
              :state="partyPreset === 'any' ? 'primary' : 'secondary'"
              :variant="partyPreset === 'any' ? 'elevated' : 'outlined'"
              size="small"
              title="Any (99)"
              type="button"
              @clicked="setPartyPreset('any')"
            />
            <BaseButton
              :state="partyPreset === 'custom' ? 'primary' : 'secondary'"
              :variant="partyPreset === 'custom' ? 'elevated' : 'outlined'"
              size="small"
              title="Custom"
              type="button"
              @clicked="setPartyPreset('custom')"
            />
          </div>
          <input
            v-model.number="form.MaxNumberOfParticipants"
            :disabled="isInputDisabled"
            inputmode="numeric"
            max="99"
            min="1"
            pattern="[0-9]*"
            placeholder="Enter custom value"
            required
            type="number"
          >
        </div>
        <div v-if="isEditMode" class="form-row">
          <label>Organizer</label>
          <input :value="user?.PlayerName || ''" disabled type="text">
        </div>
      </section>

      <!-- Actions -->
      <div class="actions">
        <BaseButton
          :disabled="loading"
          :title="loading ? (isEditMode ? 'Updating...' : 'Creating...') : (isEditMode ? 'Update' : 'Create')"
          type="submit"
        />
        <BaseButton :disabled="loading" state="secondary" title="Cancel" variant="outlined" @clicked="cancel" />
      </div>
    </form>
  </section>
</template>

<style scoped>
.page {
  max-width: 780px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 2.5rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  color: var(--fg);
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.02em;
}

.event-form {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.info-section {
  margin-bottom: 0.75rem;
}

.media-section {
  margin-bottom: 2.25rem;
}

.schedule-section {
  margin-bottom: 0.75rem;
}

.participants-section {
  margin-bottom: 0;
}

.form-section {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 16px;
  padding: 1.25rem 1.5rem 1.5rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

:root[data-theme='dark'] .form-section {
  background: rgba(18, 26, 45, 0.7);
  border: 2px solid rgba(255, 255, 255, 0.15);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .form-section {
    background: rgba(18, 26, 45, 0.7);
    border: 2px solid rgba(255, 255, 255, 0.15);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.form-section:hover {
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 6px 20px rgba(59, 130, 246, 0.12),
    0 4px 16px rgba(0, 0, 0, 0.1),
    inset 0 1px 0 rgba(255, 255, 255, 0.6);
}

:root[data-theme='dark'] .form-section:hover {
  border-color: rgba(59, 130, 246, 0.5);
  box-shadow: 0 6px 20px rgba(59, 130, 246, 0.2),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .form-section:hover {
    border-color: rgba(59, 130, 246, 0.5);
    box-shadow: 0 6px 20px rgba(59, 130, 246, 0.2),
      0 4px 16px rgba(0, 0, 0, 0.4),
      inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
}

.section-header {
  margin: 0 0 1.25rem 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--fg);
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  margin-bottom: 1rem;
}

.form-row:last-child {
  margin-bottom: 0;
}

.form-row-group {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1rem;
  margin-bottom: 1rem;
}

/* Duration: presets + input on one line */
.duration-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.duration-presets {
  display: flex;
  gap: 0.375rem;
  flex-shrink: 0;
}

.duration-input {
  width: 5.5rem;
  flex-shrink: 0;
}

/* Media: input + preview side by side */
.media-row {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 1.25rem;
  align-items: start;
}

.media-input {
  min-width: 0;
}

.image-preview {
  width: 180px;
  height: 100px;
  border: 1px solid rgba(var(--color-border), 0.5);
  border-radius: 10px;
  overflow: hidden;
  background: var(--muted-bg);
  flex-shrink: 0;
}

.image-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

/* Party presets: flex-wrap for natural sizing */
.party-preset-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  margin-bottom: 0.625rem;
}

/* Actions: anchored with a divider */
.actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 1.5rem;
  margin-top: 0.5rem;
  border-top: 1px solid var(--border);
}

label {
  font-weight: 500;
  font-size: 0.9rem;
  color: var(--fg);
}

.error {
  padding: 0.875rem 1rem;
  background: var(--alert-error-bg);
  color: var(--alert-error-fg);
  border: 1px solid var(--alert-error-border);
  border-radius: 12px;
  margin-bottom: 1.5rem;
}

.hint {
  font-size: 0.8125rem;
  color: var(--muted);
  margin-top: 0.125rem;
}

@media (max-width: 768px) {
  .page {
    max-width: 100%;
  }

  .form-section {
    padding: 1rem 1.25rem 1.25rem;
  }

  .duration-controls {
    flex-wrap: wrap;
  }

  .media-row {
    grid-template-columns: 1fr;
  }

  .image-preview {
    width: 100%;
    max-width: 100%;
    height: auto;
    aspect-ratio: 16 / 9;
  }

  .form-row-group {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 480px) {
  .page-header {
    margin-bottom: 1.75rem;
  }

  .form-section {
    padding: 0.875rem 1rem 1rem;
  }

  .section-header {
    font-size: 1rem;
    margin-bottom: 1rem;
  }

  .duration-presets {
    flex-wrap: wrap;
  }

  .actions {
    flex-direction: column;
  }
}
</style>
