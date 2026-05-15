<script lang="ts" setup>
import type { FCEvent } from '@/features/events/events.types'
import type { Fight } from '@/features/fights/fights.types'
import type { RecurrenceConfig } from '@/utils/ical'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import DateTimePicker from '@/components/DateTimePicker.vue'
import EventCard from '@/components/events/EventCard.vue'
import RecurrenceOptions from '@/components/events/RecurrenceOptions.vue'
import SearchableDropdown from '@/components/SearchableDropdown.vue'
import { useAuth } from '@/composables/useAuth'
import { EventsApi } from '@/features/events/events.api'
import { EventType, OccurrenceStatus } from '@/features/events/events.types'
import { FightsApi } from '@/features/fights/fights.api'
import { fightTypeToString } from '@/features/fights/fights.types'
import mapsPlaceholder from '@/static/img/maps-placeholder.png'
import { generateICalString, parseICalString } from '@/utils/ical'

const router = useRouter()
const route = useRoute()
const { user, isAdmin, loadMe } = useAuth()

function toAbsoluteUrl(url: string): string {
  if (!url)
    return url
  if (/^(?:[a-z]+:)?\/\//i.test(url))
    return url
  const withSlash = url.startsWith('/') ? url : `/${url}`
  return `${window.location.origin}${withSlash}`
}

const mapsPlaceholderAbsolute = toAbsoluteUrl(mapsPlaceholder)

const loading = ref(false)
const error = ref('')
const isEditMode = computed(() => !!route.params.id)

const fights = ref<Fight[]>([])
const selectedFight = ref<Fight | null>(null)

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
  Organizer: '',
  StartDate: new Date(),
  EndDate: new Date(),
  Duration: 30,
  ICalString: '',
  SignupType: 0,
  MaxNumberOfParticipants: 8,
  Signups: [],
  Groups: [],
  Occurrences: [],
  AvailableForSignup: false,
  IsArchived: false,
  CanBeArchived: false,
})

const recurrence = ref<RecurrenceConfig>({
  enabled: false,
  frequency: 'WEEKLY',
  interval: 1,
  endType: 'never',
  byWeekday: [],
})

const previewCollapsed = ref(true)

const previewEvent = computed<FCEvent>(() => {
  const icalStr = generateICalString(form, recurrence.value)
  return {
    ...form,
    Organizer: form.Organizer || user.value?.PlayerName || 'You',
    ICalString: icalStr,
    AvailableForSignup: true,
    Occurrences: [{
      Id: 'preview',
      OccurrenceDate: form.StartDate,
      Status: OccurrenceStatus.Scheduled,
    }],
    IsArchived: false,
    CanBeArchived: false,
  }
})

const previewEventRef = ref<FCEvent>(previewEvent.value)
watch(previewEvent, (val) => { previewEventRef.value = { ...val } }, { deep: true })

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
      if (form.MaxNumberOfParticipants === 4 || form.MaxNumberOfParticipants === 8
        || form.MaxNumberOfParticipants === 24 || form.MaxNumberOfParticipants === 99) {
        form.MaxNumberOfParticipants = 0
      }
      break
  }
}

function setDuration(minutes: number) {
  form.Duration = minutes
}

function detectPreset(maxParticipants: number): PartyPreset {
  switch (maxParticipants) {
    case 4: return 'light-party'
    case 8: return 'full-party'
    case 24: return 'alliance-raid'
    case 99: return 'any'
    default: return 'custom'
  }
}

const isInputDisabled = computed(() => partyPreset.value !== 'custom')

const partyPresetOptions = [
  { key: 'light-party' as PartyPreset, label: 'Light (4)' },
  { key: 'full-party' as PartyPreset, label: 'Full (8)' },
  { key: 'alliance-raid' as PartyPreset, label: 'Alliance (24)' },
  { key: 'any' as PartyPreset, label: 'Any (99)' },
  { key: 'custom' as PartyPreset, label: 'Custom' },
]

const eventTypeOptions = computed(() => {
  return Object.keys(EventType)
    .filter(key => Number.isNaN(Number(key)))
    .map(key => ({
      value: EventType[key as keyof typeof EventType],
      label: key,
    }))
})

const showFightSelection = computed(() => {
  const fightCompatibleTypes = [
    EventType.Academy, EventType.Downsynced, EventType.BLU,
    EventType.Farming, EventType.Raid, EventType.MinIlvl, EventType.Other,
  ]
  return fightCompatibleTypes.includes(form.Type)
})

function formatFight(fight: Fight): string {
  return `${fight.Name} (${fightTypeToString(fight.Type)})`
}

onMounted(async () => {
  try {
    fights.value = await FightsApi.list()
  }
  catch {
    // Fights are optional
  }

  if (isEditMode.value) {
    loading.value = true
    try {
      const eventData = await EventsApi.get(route.params.id as string)
      if (eventData) {
        Object.assign(form, eventData)
        if (form.PictureUrl) {
          form.PictureUrl = toAbsoluteUrl(form.PictureUrl)
        }
        partyPreset.value = detectPreset(eventData.MaxNumberOfParticipants)
        if (eventData.FightId) {
          selectedFight.value = fights.value.find(f => f.Id === eventData.FightId) || null
        }
        if (eventData.ICalString) {
          const parsedRecurrence = parseICalString(eventData.ICalString)
          if (parsedRecurrence) {
            recurrence.value = parsedRecurrence
          }
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

watch(
  () => form.Type,
  (newType) => {
    if (newType === EventType.Maps) {
      if (!form.PictureUrl || form.PictureUrl === mapsPlaceholder || form.PictureUrl === mapsPlaceholderAbsolute) {
        form.PictureUrl = mapsPlaceholderAbsolute
      }
    }
    else {
      if ((form.PictureUrl === mapsPlaceholder || form.PictureUrl === mapsPlaceholderAbsolute) && !selectedFight.value) {
        form.PictureUrl = ''
      }
    }
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
    form.Organizer = user.value?.PlayerName ?? ''
    if (form.PictureUrl) {
      form.PictureUrl = toAbsoluteUrl(form.PictureUrl)
    }
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
  <div class="create-event-page">
    <div class="page-header">
      <h2 class="page-title">
        {{ isEditMode ? 'Edit Event' : 'New Event' }}
      </h2>
    </div>

    <p v-if="error" class="form-error">
      {{ error }}
    </p>

    <div class="create-event-layout">
      <!-- Form Column -->
      <form class="event-form" @submit.prevent="submit">
        <div class="form-surface">
          <!-- Basic Information -->
          <div class="form-group">
            <div class="form-field">
              <label for="event-name">Name</label>
              <input id="event-name" v-model="form.Name" placeholder="Event name" required type="text">
            </div>

            <div class="form-field">
              <label for="event-description">Description</label>
              <textarea id="event-description" v-model="form.Description" placeholder="Describe the event (supports Discord formatting)" rows="4" />
            </div>

            <div class="form-field-row">
              <div class="form-field">
                <label for="event-type">Type</label>
                <select id="event-type" v-model.number="form.Type" required>
                  <option v-for="option in eventTypeOptions" :key="option.value" :value="option.value">
                    {{ option.label }}
                  </option>
                </select>
              </div>
              <div v-if="form.DiscordMessageId" class="form-field">
                <label for="discord-id">Discord Message ID</label>
                <input id="discord-id" v-model="form.DiscordMessageId" placeholder="Discord message ID" type="text">
              </div>
            </div>
          </div>

          <hr class="form-divider">

          <!-- Fight & Media -->
          <div class="form-group">
            <h3 class="form-group-label">
              {{ showFightSelection ? 'Fight & Media' : 'Media' }}
            </h3>
            <div v-if="showFightSelection" class="form-field">
              <label>Fight (optional)</label>
              <SearchableDropdown
                v-model="selectedFight"
                :format-option="formatFight"
                :options="fights"
                placeholder="Search fights..."
              />
              <small class="field-hint">Auto-fills the event image</small>
            </div>
            <div class="form-field">
              <label for="picture-url">Image URL</label>
              <input
                id="picture-url"
                v-model="form.PictureUrl"
                :placeholder="showFightSelection ? 'Auto-filled from fight' : 'https://...'"
                type="url"
              >
            </div>
          </div>

          <hr class="form-divider">

          <!-- Schedule -->
          <div class="form-group">
            <h3 class="form-group-label">
              Schedule
            </h3>
            <div class="form-field">
              <DateTimePicker
                v-model="form.StartDate"
                :required="true"
                label="Start Date & Time"
              />
            </div>
            <div class="form-field">
              <label>Duration</label>
              <div class="duration-controls">
                <div class="duration-presets">
                  <BaseButton
                    v-for="mins in [60, 120, 180]"
                    :key="mins"
                    :state="form.Duration === mins ? 'primary' : 'secondary'"
                    :variant="form.Duration === mins ? 'elevated' : 'outlined'"
                    :title="`${mins} min`"
                    size="small"
                    type="button"
                    @clicked="setDuration(mins)"
                  />
                </div>
                <input
                  v-model.number="form.Duration"
                  class="duration-input"
                  inputmode="numeric"
                  min="0"
                  pattern="[0-9]*"
                  placeholder="min"
                  required
                  type="number"
                >
              </div>
            </div>
            <div class="form-field">
              <RecurrenceOptions v-model="recurrence" />
            </div>
          </div>

          <hr class="form-divider">

          <!-- Participants -->
          <div class="form-group">
            <h3 class="form-group-label">
              Participants
            </h3>
            <div class="form-field">
              <label>Party Size</label>
              <div class="party-presets">
                <BaseButton
                  v-for="preset in partyPresetOptions"
                  :key="preset.key"
                  :state="partyPreset === preset.key ? 'primary' : 'secondary'"
                  :variant="partyPreset === preset.key ? 'elevated' : 'outlined'"
                  :title="preset.label"
                  size="small"
                  type="button"
                  @clicked="setPartyPreset(preset.key)"
                />
              </div>
              <input
                v-if="partyPreset === 'custom'"
                v-model.number="form.MaxNumberOfParticipants"
                :disabled="isInputDisabled"
                inputmode="numeric"
                max="99"
                min="1"
                pattern="[0-9]*"
                placeholder="Custom count"
                required
                type="number"
              >
            </div>
            <div v-if="isEditMode" class="form-field">
              <label>Organizer</label>
              <input :value="user?.PlayerName || ''" disabled type="text">
            </div>
          </div>
        </div>

        <!-- Form Actions -->
        <div class="form-actions">
          <BaseButton :disabled="loading" state="secondary" title="Cancel" variant="outlined" @clicked="cancel" />
          <BaseButton
            :disabled="loading"
            :title="loading ? (isEditMode ? 'Saving...' : 'Creating...') : (isEditMode ? 'Save Changes' : 'Create Event')"
            type="submit"
          />
        </div>
      </form>

      <!-- Preview Column (desktop) -->
      <aside class="preview-column">
        <div class="preview-sticky">
          <span class="preview-label">Live Preview</span>
          <div class="preview-card-wrap">
            <EventCard
              v-model:fc-event="previewEventRef"
              :is-member="true"
            />
          </div>
        </div>
      </aside>
    </div>

    <!-- Preview Toggle (mobile) -->
    <div class="preview-mobile">
      <button class="preview-toggle" type="button" @click="previewCollapsed = !previewCollapsed">
        <span>{{ previewCollapsed ? 'Show Preview' : 'Hide Preview' }}</span>
        <svg
          :class="{ 'chevron--open': !previewCollapsed }"
          class="chevron"
          fill="none"
          height="16"
          stroke="currentColor"
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          viewBox="0 0 24 24"
          width="16"
        >
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </button>
      <div v-if="!previewCollapsed" class="preview-mobile-card">
        <EventCard
          v-model:fc-event="previewEventRef"
          :is-member="true"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.create-event-page {
  max-width: 1120px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  color: var(--fg);
  letter-spacing: -0.02em;
}

.form-error {
  padding: 0.75rem 1rem;
  background: var(--alert-error-bg);
  color: var(--alert-error-fg);
  border: 1px solid var(--alert-error-border);
  border-radius: 12px;
  margin-bottom: 1.5rem;
}

/* Two-column layout */
.create-event-layout {
  display: grid;
  grid-template-columns: 1fr;
  gap: 2rem;
}

@media (min-width: 960px) {
  .create-event-layout {
    grid-template-columns: 1fr 340px;
  }
}

/* Form */
.event-form {
  min-width: 0;
}

.form-surface {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 16px;
  padding: 1.5rem;
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
}

:root[data-theme='dark'] .form-surface {
  background: rgba(18, 26, 45, 0.7);
  border-color: rgba(255, 255, 255, 0.15);
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .form-surface {
    background: rgba(18, 26, 45, 0.7);
    border-color: rgba(255, 255, 255, 0.15);
    box-shadow:
      0 4px 16px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.form-group-label {
  font-size: 0.8125rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--muted);
  margin: 0;
}

.form-divider {
  border: none;
  border-top: 1px solid var(--border);
  margin: 1.25rem 0;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.form-field label {
  font-weight: 500;
  font-size: 0.875rem;
  color: var(--fg);
}

.form-field-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 0.75rem;
}

.field-hint {
  font-size: 0.75rem;
  color: var(--muted);
}

/* Duration */
.duration-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.duration-presets {
  display: flex;
  gap: 0.25rem;
  flex-shrink: 0;
}

.duration-input {
  width: 5rem;
  flex-shrink: 0;
}

/* Party presets */
.party-presets {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem;
}

/* Form actions */
.form-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 1.25rem;
}

/* Preview column (desktop) */
.preview-column {
  display: none;
}

@media (min-width: 960px) {
  .preview-column {
    display: block;
  }
}

.preview-sticky {
  position: sticky;
  top: 1.5rem;
}

.preview-label {
  display: block;
  font-size: 0.6875rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--muted);
  margin-bottom: 0.5rem;
}

.preview-card-wrap {
  pointer-events: none;
}

.preview-card-wrap :deep(.event-card__actions) {
  display: none;
}

/* Preview mobile */
.preview-mobile {
  margin-top: 1.5rem;
}

@media (min-width: 960px) {
  .preview-mobile {
    display: none;
  }
}

.preview-toggle {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  padding: 0.75rem 1rem;
  background: var(--muted-bg);
  border: 1px solid var(--border);
  border-radius: 12px;
  color: var(--fg);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s ease;
}

.preview-toggle:hover {
  background: color-mix(in oklab, var(--muted-bg) 85%, var(--link) 15%);
}

.chevron {
  margin-left: auto;
  transition: transform 0.2s ease;
}

.chevron--open {
  transform: rotate(180deg);
}

.preview-mobile-card {
  margin-top: 0.75rem;
  pointer-events: none;
}

.preview-mobile-card :deep(.event-card__actions) {
  display: none;
}

/* Responsive */
@media (max-width: 768px) {
  .form-surface {
    padding: 1rem 1.25rem 1.25rem;
  }

  .duration-controls {
    flex-wrap: wrap;
  }

  .form-field-row {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 480px) {
  .page-header {
    margin-bottom: 1.5rem;
  }

  .form-surface {
    padding: 0.75rem 1rem 1rem;
    border-radius: 12px;
  }

  .form-group-label {
    font-size: 0.75rem;
  }

  .duration-presets {
    flex-wrap: wrap;
  }

  .form-actions {
    flex-direction: column;
  }
}
</style>
