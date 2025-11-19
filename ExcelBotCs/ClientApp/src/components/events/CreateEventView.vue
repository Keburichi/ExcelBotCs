<script setup lang="ts">
import type { FCEvent } from '@/features/events/events.types'
import type { Fight } from '@/features/fights/fights.types'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import DateTimePicker from '@/components/DateTimePicker.vue'
import SearchableDropdown from '@/components/SearchableDropdown.vue'
import { useAuth } from '@/composables/useAuth'
import { EventsApi } from '@/features/events/events.api'
import { EventType } from '@/features/events/events.types'
import { FightsApi } from '@/features/fights/fights.api'
import { fightTypeToString } from '@/features/fights/fights.types'
import mapsPlaceholder from '@/static/img/maps-placeholder.png'

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
  Duration: 0,
  MaxNumberOfParticipants: 8,
  Signups: [],
  Participants: [],
  AvailableForSignup: false,
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
    .filter(key => isNaN(Number(key)))
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
    <h2>{{ isEditMode ? 'Edit FC Event' : 'Create FC Event' }}</h2>
    <p v-if="error" class="error">
      {{ error }}
    </p>
    <form class="form" @submit.prevent="submit">
      <div class="form-row">
        <label>Name</label>
        <input v-model="form.Name" type="text" required placeholder="Event name">
      </div>
      <div class="form-row">
        <label>Description</label>
        <textarea v-model="form.Description" rows="5" placeholder="Describe the event" />
      </div>
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
        <input v-model="form.DiscordMessageId" type="text" placeholder="The message id of the discord post.">
      </div>
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
      <div class="form-row">
        <label>Picture URL (optional)</label>
        <input v-model="form.PictureUrl" placeholder="https://... (auto-filled from fight if selected)" type="url">
      </div>
      <div v-if="form.PictureUrl" class="form-row">
        <label>Preview</label>
        <div class="image-preview">
          <img :src="form.PictureUrl" alt="Event preview" @error="(e) => (e.target as HTMLImageElement).style.display = 'none'">
        </div>
      </div>
      <div class="form-row">
        <DateTimePicker
          v-model="form.StartDate"
          :required="true"
          label="Start Date & Time"
        />
      </div>
      <div class="form-row">
        <label>Duration (minutes)</label>
        <input v-model.number="form.Duration" type="number" min="0" required placeholder="e.g. 120 for 2 hours">
      </div>
      <div class="form-row">
        <label>Max Number of Participants</label>
        <div class="party-preset-buttons">
          <BaseButton
            title="Light Party (4)"
            type="button"
            :state="partyPreset === 'light-party' ? 'primary' : 'secondary'"
            :variant="partyPreset === 'light-party' ? 'elevated' : 'outlined'"
            @clicked="setPartyPreset('light-party')"
          />

          <BaseButton
            title="Full Party (8)"
            type="button"
            :state="partyPreset === 'full-party' ? 'primary' : 'secondary'"
            :variant="partyPreset === 'full-party' ? 'elevated' : 'outlined'"
            @clicked="setPartyPreset('full-party')"
          />
          <BaseButton
            title="Alliance Raid (24)"
            type="button"
            :state="partyPreset === 'alliance-raid' ? 'primary' : 'secondary'"
            :variant="partyPreset === 'alliance-raid' ? 'elevated' : 'outlined'"
            @clicked="setPartyPreset('alliance-raid')"
          />
          <BaseButton
            title="Any (99)"
            type="button"
            :state="partyPreset === 'any' ? 'primary' : 'secondary'"
            :variant="partyPreset === 'any' ? 'elevated' : 'outlined'"
            @clicked="setPartyPreset('any')"
          />
          <BaseButton
            title="Custom"
            type="button"
            :state="partyPreset === 'custom' ? 'primary' : 'secondary'"
            :variant="partyPreset === 'custom' ? 'elevated' : 'outlined'"
            @clicked="setPartyPreset('custom')"
          />
        </div>
        <input
          v-model.number="form.MaxNumberOfParticipants"
          type="number"
          min="1"
          max="99"
          required
          placeholder="Enter custom value"
          :disabled="isInputDisabled"
        >
      </div>
      <div v-if="isEditMode" class="form-row">
        <label>Organizer</label>
        <input :value="user?.PlayerName || ''" type="text" disabled>
      </div>
      <div class="actions">
        <BaseButton
          :title="loading ? (isEditMode ? 'Updating...' : 'Creating...') : (isEditMode ? 'Update' : 'Create')"
          :disabled="loading"
          type="submit"
        />

        <BaseButton title="Cancel" :disabled="loading" state="secondary" variant="outlined" @clicked="cancel" />
      </div>
    </form>
  </section>
</template>

<style scoped>
.page {
  max-width: 720px;
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 12px 0;
}

.image-preview {
  width: 100%;
  max-width: 500px;
  border: 1px solid var(--border);
  border-radius: 12px;
  overflow: hidden;
  background: var(--muted-bg);
}

.image-preview img {
  width: 100%;
  height: auto;
  display: block;
}

.error {
  color: #c62828;
}

.hint {
  font-size: 0.875rem;
  color: var(--muted);
  font-style: italic;
  margin-top: 4px;
}

/* Party preset buttons */
.party-preset-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

/* Responsive layout for preset buttons */
@media (max-width: 640px) {
  .party-preset-buttons {
    flex-direction: column;
  }
}
</style>
