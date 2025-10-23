<script setup lang="ts">
import type { FCEvent } from '@/features/events/events.types'
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import { useAuth } from '@/composables/useAuth'
import { EventsApi } from '@/features/events/events.api'

const router = useRouter()
const route = useRoute()
const { user, isAdmin, loadMe } = useAuth()

const loading = ref(false)
const error = ref('')
const isEditMode = computed(() => !!route.params.id)

// Party size presets
type PartyPreset = 'light-party' | 'full-party' | 'alliance-raid' | 'any' | 'custom'
const partyPreset = ref<PartyPreset>('full-party')

const form = reactive<FCEvent>({
  Id: '',
  Name: '',
  Description: '',
  DiscordMessageId: '',
  PictureUrl: '',
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

// Load event data if in edit mode
onMounted(async () => {
  if (isEditMode.value) {
    loading.value = true
    try {
      const eventData = await EventsApi.get(route.params.id as string)
      if (eventData) {
        Object.assign(form, eventData)
        // Detect and set the appropriate preset
        partyPreset.value = detectPreset(eventData.MaxNumberOfParticipants)
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

// Convert Date to datetime-local format (YYYY-MM-DDTHH:mm)
const localStartDate = computed({
  get: () => {
    const date = new Date(form.StartDate)
    // Format: YYYY-MM-DDTHH:mm (local timezone for display)
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    const hours = String(date.getHours()).padStart(2, '0')
    const minutes = String(date.getMinutes()).padStart(2, '0')
    return `${year}-${month}-${day}T${hours}:${minutes}`
  },
  set: (value: string) => {
    // Parse local datetime and convert to UTC Date
    const localDate = new Date(value)
    form.StartDate = localDate
  },
})

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
      <div v-if="form.DiscordMessageId" class="form-row">
        <label>Discord Message Id</label>
        <input v-model="form.DiscordMessageId" type="text" placeholder="The message id of the discord post.">
      </div>
      <div class="form-row">
        <label>Picture URL (optional)</label>
        <input v-model="form.PictureUrl" type="url" placeholder="https://...">
      </div>
      <div v-if="form.PictureUrl" class="form-row">
        <label>Preview</label>
        <div class="image-preview">
          <img :src="form.PictureUrl" alt="Event preview" @error="(e) => (e.target as HTMLImageElement).style.display = 'none'">
        </div>
      </div>
      <div class="form-row">
        <label>Start Date & Time (your local timezone)</label>
        <input v-model="localStartDate" type="datetime-local" required>
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
