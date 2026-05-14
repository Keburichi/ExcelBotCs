<script lang="ts" setup>
import type { EventOccurrence, FCEvent } from '@/features/events/events.types'
import type { Fight } from '@/features/fights/fights.types'
import { computed, onMounted, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseCard from '@/components/BaseCard.vue'
import BaseModal from '@/components/BaseModal.vue'
import DiscordMessageRenderer from '@/components/DiscordMessageRenderer.vue'
import ConcludeEventDialog from '@/components/events/ConcludeEventDialog.vue'
import EventOrganizationDialog from '@/components/events/EventOrganizationDialog.vue'
import EventSignupDialog from '@/components/events/EventSignupDialog.vue'
import ExtendEventDialog from '@/components/events/ExtendEventDialog.vue'
import RaidplanDialog from '@/components/fights/RaidplanDialog.vue'
import { useEvents } from '@/composables/useEvents'
import { eventTypeToString, OccurrenceStatus, occurrenceStatusToString } from '@/features/events/events.types'
import { FightsApi } from '@/features/fights/fights.api'
import { describeRecurrence, isRecurring, parseICalString } from '@/utils/ical'

const props = defineProps<{
  isMember?: boolean
  isAdmin?: boolean
  isArchiveView?: boolean
  isDeveloper?: boolean
}>()

const emit = defineEmits<{
  startEdit: [event: FCEvent]
  cancelEdit: []
  saveEdit: []
  deleteEvent: [event: FCEvent]
  cardClick: [event: FCEvent]
  extended: [event: FCEvent]
  archived: [event: FCEvent]
}>()

const fcEventValue = defineModel<FCEvent>('fcEvent', { required: true })

const isOpen = ref(false)
const isOrganizationOpen = ref(false)
const isDeleteOpen = ref(false)
const isConcludeOpen = ref(false)
const isRaidplanDialogOpen = ref(false)
const isExtendOpen = ref(false)

const { getNextOccurrence, getOccurrenceToComplete, getEvent, updateOccurrenceStatusById } = useEvents()

// Fights data for lookup
const fights = ref<Fight[]>([])

// Get next occurrence for display (future scheduled occurrences only)
const nextOccurrence = computed((): EventOccurrence | null => {
  return getNextOccurrence(fcEventValue.value)
})

// Get occurrence that can be completed (prioritizes past scheduled occurrences)
const occurrenceToComplete = computed((): EventOccurrence | null => {
  return getOccurrenceToComplete(fcEventValue.value)
})

// Get past scheduled occurrence (for skip functionality)
const pastScheduledOccurrence = computed((): EventOccurrence | null => {
  if (!fcEventValue.value.Occurrences)
    return null

  const now = new Date()
  return fcEventValue.value.Occurrences
    .filter(o =>
      o.Status === OccurrenceStatus.Scheduled
      && new Date(o.OccurrenceDate) < now,
    )
    .sort((a, b) => new Date(a.OccurrenceDate).getTime() - new Date(b.OccurrenceDate).getTime())[0] || null
})

// Computed properties for display
const eventTypeLabel = computed(() => {
  return fcEventValue.value.Type !== undefined ? eventTypeToString(fcEventValue.value.Type) : null
})

const associatedFight = computed(() => {
  if (!fcEventValue.value.FightId || fights.value.length === 0) {
    return null
  }
  return fights.value.find(f => f.Id === fcEventValue.value.FightId)
})

// Duration and time formatting
const formattedDuration = computed(() => {
  const minutes = fcEventValue.value.Duration
  if (minutes < 60) {
    return `${minutes} min`
  }
  const hours = Math.floor(minutes / 60)
  const remainingMinutes = minutes % 60
  if (remainingMinutes === 0) {
    return `${hours}h`
  }
  return `${hours}h ${remainingMinutes}min`
})

// Compact local time display: "Mon, Jan 15, 2025, 8:00 PM - 10:00 PM (2h)"
const localTimeRange = computed(() => {
  // Use next occurrence date if available, otherwise fall back to event start date
  const occurrenceDate = nextOccurrence.value?.OccurrenceDate ?? fcEventValue.value.StartDate
  const startDate = new Date(occurrenceDate)
  const endDate = new Date(startDate.getTime() + fcEventValue.value.Duration * 60 * 1000)

  const dateStr = startDate.toLocaleString(undefined, {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })

  const startTime = startDate.toLocaleString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })

  const endTime = endDate.toLocaleString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })

  return `${dateStr}, ${startTime} - ${endTime} (${formattedDuration.value})`
})

// Compact server time display: "8:00 PM - 10:00 PM (ST)"
const serverTimeRange = computed(() => {
  const occurrenceDate = nextOccurrence.value?.OccurrenceDate ?? fcEventValue.value.StartDate
  const startDate = new Date(occurrenceDate)
  const endDate = new Date(startDate.getTime() + fcEventValue.value.Duration * 60 * 1000)

  const startTime = startDate.toLocaleString('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'UTC',
  })

  const endTime = endDate.toLocaleString('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'UTC',
  })

  return `${startTime} - ${endTime} (ST)`
})

// Recurrence information
const eventIsRecurring = computed(() => {
  return fcEventValue.value.ICalString && isRecurring(fcEventValue.value.ICalString)
})

const recurrenceDescription = computed(() => {
  if (!eventIsRecurring.value)
    return ''
  const config = parseICalString(fcEventValue.value.ICalString)
  return config ? describeRecurrence(config, fcEventValue.value.ICalString) : ''
})

// Handle EventSignupDialog close - fetch updated event data
async function handleSignupDialogClose(value: boolean) {
  isOpen.value = value

  // When dialog closes (value becomes false), fetch updated event data
  if (!value) {
    const updatedEvent = await getEvent(fcEventValue.value.Id)
    if (updatedEvent) {
      fcEventValue.value = updatedEvent
    }
  }
}

function getSignUpNumber(fcEvent: FCEvent) {
  if (!fcEvent.Signups)
    return 0

  return fcEvent.Signups.filter(signup => signup.Roles.length > 0).length
}

function getParticipantCount(fcEvent: FCEvent) {
  if (!fcEvent.Groups || fcEvent.Groups.length === 0)
    return 0
  return fcEvent.Groups.flatMap(g => g.Participants).length
}

function openFightResources() {
  isRaidplanDialogOpen.value = true
}

// Handle event concluded - reload event data and check for auto-archive
async function handleEventConcluded() {
  const updatedEvent = await getEvent(fcEventValue.value.Id)
  if (updatedEvent) {
    fcEventValue.value = updatedEvent

    // If the event was auto-archived, notify parent to remove from list
    if (updatedEvent.IsArchived) {
      emit('archived', updatedEvent)
    }
  }
}

// Skip past occurrence
async function skipPastOccurrence() {
  if (!pastScheduledOccurrence.value)
    return

  try {
    await updateOccurrenceStatusById(
      fcEventValue.value.Id,
      pastScheduledOccurrence.value.Id,
      OccurrenceStatus.Cancelled,
    )

    // Reload event data
    const updatedEvent = await getEvent(fcEventValue.value.Id)
    if (updatedEvent) {
      fcEventValue.value = updatedEvent

      // If the event was auto-archived, notify parent to remove from list
      if (updatedEvent.IsArchived) {
        emit('archived', updatedEvent)
      }
    }
  }
  catch (error) {
    console.error('Error skipping occurrence:', error)
    alert('Failed to skip occurrence. Please try again.')
  }
}

// Handle extend event
function handleExtended(updatedEvent: FCEvent) {
  fcEventValue.value = updatedEvent
  emit('extended', updatedEvent)
}

// Load fights on mount for lookup
onMounted(async () => {
  try {
    fights.value = await FightsApi.list()
  }
  catch (error) {
    console.error('Failed to load fights:', error)
  }
})
</script>

<template>
  <EventSignupDialog :event="fcEventValue" :model-value="isOpen" @update:model-value="handleSignupDialogClose" />

  <EventOrganizationDialog
    v-model:fc-event="fcEventValue"
    v-model:is-open="isOrganizationOpen"
    @event-planned="handleSignupDialogClose(false)"
  />

  <ConcludeEventDialog
    v-if="occurrenceToComplete"
    v-model="isConcludeOpen"
    :event="fcEventValue"
    :occurrence="occurrenceToComplete"
    @concluded="handleEventConcluded"
    @skipped="handleEventConcluded"
  />

  <BaseModal v-model="isDeleteOpen" :title="`Deleting Event - ${fcEventValue.Name}`">
    <template #body>
      <p>Are you sure you want to delete this event?</p>
    </template>
    <template #actions>
      <BaseButton title="Cancel" @clicked="isDeleteOpen = false" />
      <BaseButton state="danger" title="Yes, delete this!" @clicked="emit('deleteEvent', fcEventValue)" />
    </template>
  </BaseModal>

  <ExtendEventDialog
    v-if="eventIsRecurring && !props.isArchiveView"
    v-model="isExtendOpen"
    :event="fcEventValue"
    @extended="handleExtended"
  />

  <BaseCard :title="fcEventValue.Name" size="large" title-class="text-2xl font-bold" variant="elevated">
    <template #image>
      <img
        v-if="fcEventValue.PictureUrl" :src="fcEventValue.PictureUrl" alt="avatar" class="card__image"
        referrerpolicy="no-referrer"
      >
      <span v-else class="card__image placeholder">?</span>
    </template>
    <template #body>
      <DiscordMessageRenderer :content="fcEventValue.Description" />
    </template>
    <template #footer>
      <div v-if="eventTypeLabel || associatedFight || fcEventValue.IsArchived" class="event-metadata">
        <span v-if="fcEventValue.IsArchived" class="archived-badge">
          ARCHIVED
        </span>
        <span v-if="eventTypeLabel" :class="`type-${eventTypeLabel.toLowerCase()}`" class="event-type-badge">
          {{ eventTypeLabel }}
        </span>
        <span v-if="associatedFight" class="fight-info">
          <strong>Fight:</strong>
          <BaseButton
            :title="associatedFight.Name"
            :tooltip="`View ${associatedFight.Name} resources`"
            variant="text"
            @clicked="openFightResources"
          />
        </span>
      </div>
      <div class="event-datetime">
        <div class="datetime-row">
          <span class="datetime-label">Local Time:</span>
          <span class="datetime-value">{{ localTimeRange }}</span>
        </div>
        <div class="datetime-row">
          <span class="datetime-label">Server Time:</span>
          <span class="datetime-value">{{ serverTimeRange }}</span>
        </div>
      </div>
      <div v-if="eventIsRecurring" class="recurrence-info">
        <span class="recurrence-icon">🔄</span>
        <span class="recurrence-text">{{ recurrenceDescription }}</span>
      </div>
      <div v-if="nextOccurrence" class="occurrence-status">
        <span class="status-label">Status:</span>
        <span class="status-value">{{ occurrenceStatusToString(nextOccurrence.Status) }}</span>
        <span class="participants-info">{{ getParticipantCount(fcEventValue) }}/{{
          fcEventValue.MaxNumberOfParticipants
        }} selected</span>
      </div>
      <p>Organized by: {{ fcEventValue.Organizer }}</p>
      <div v-if="!props.isArchiveView" class="actions">
        <BaseButton
          :disabled="!props.isMember || !fcEventValue.AvailableForSignup"
          :title="`Sign up (${getSignUpNumber(fcEventValue)})`"
          size="small"
          tooltip="Sign up for this event"
          @clicked="isOpen = true"
        />
        <BaseButton
          v-if="props.isAdmin && fcEventValue.AvailableForSignup && nextOccurrence" size="small" state="secondary"
          title="Select Participants" @clicked="isOrganizationOpen = true"
        />
        <BaseButton
          v-if="props.isAdmin && pastScheduledOccurrence"
          :tooltip="`Skip occurrence from ${new Date(pastScheduledOccurrence.OccurrenceDate).toLocaleDateString()}`"
          size="small"
          state="warning"
          title="Skip Past Occurrence"
          @clicked="skipPastOccurrence"
        />
        <BaseButton
          v-if="props.isAdmin && occurrenceToComplete && occurrenceToComplete.Status !== OccurrenceStatus.Completed && occurrenceToComplete.Status !== OccurrenceStatus.Cancelled"
          size="small"
          title="Conclude Event"
          tooltip="Mark occurrence as completed and optionally award lottery guesses"
          @clicked="isConcludeOpen = true"
        />
        <BaseButton
          v-if="props.isAdmin && eventIsRecurring && !fcEventValue.IsArchived && props.isDeveloper"
          size="small"
          state="secondary"
          title="Extend"
          tooltip="Add more occurrences to this recurring event"
          @clicked="isExtendOpen = true"
        />
        <BaseButton
          v-if="props.isAdmin" size="small" state="danger" title="Delete"
          @clicked="isDeleteOpen = true"
        />
      </div>
    </template>
    <template #actions>
      <BaseButton
        v-if="props.isAdmin" size="medium" title="Edit" tooltip="Edit event"
        @clicked="emit('startEdit', fcEventValue)"
      />
    </template>
  </BaseCard>

  <!-- Raidplan Dialog -->
  <RaidplanDialog
    v-if="associatedFight"
    v-model:is-open="isRaidplanDialogOpen"
    :fight="associatedFight"
    @close="isRaidplanDialogOpen = false"
  />
</template>

<style scoped>
.card__image {
  /* zoom in on the image since the fight images have a small white gradient */
  transform: scale(1.1);
}

.event-metadata {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--border, #e0e0e0);
}

.event-type-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  background: var(--muted-bg, #f5f5f5);
  color: var(--fg, #333);
}

/* Event type specific colors */
.event-type-badge.type-raid {
  background: #e3f2fd;
  color: #1565c0;
}

.event-type-badge.type-social {
  background: #f3e5f5;
  color: #7b1fa2;
}

.event-type-badge.type-farming {
  background: #e8f5e9;
  color: #2e7d32;
}

.event-type-badge.type-maps {
  background: #fff3e0;
  color: #e65100;
}

.event-type-badge.type-blu {
  background: #e0f2f1;
  color: #00695c;
}

.event-type-badge.type-academy {
  background: #fce4ec;
  color: #c2185b;
}

.event-type-badge.type-minilvl {
  background: #fff9c4;
  color: #f57f17;
}

.event-type-badge.type-downsynced {
  background: #ede7f6;
  color: #4527a0;
}

.event-type-badge.type-other {
  background: #eceff1;
  color: #455a64;
}

.fight-info {
  font-size: 0.9rem;
  color: var(--fg, #333);
  display: flex;
  align-items: center;
  gap: 4px;
}

.fight-info strong {
  color: var(--muted, #666);
  font-weight: 500;
}

.event-datetime {
  margin-bottom: 12px;
  padding: 12px;
  background: var(--muted-bg, #f9f9f9);
  border-radius: 8px;
  border: 1px solid var(--border, #e0e0e0);
}

.datetime-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 4px 0;
}

.datetime-row:not(:last-child) {
  margin-bottom: 6px;
}

.datetime-label {
  font-weight: 600;
  color: var(--muted, #666);
  font-size: 0.9rem;
}

.datetime-value {
  font-size: 0.95rem;
  color: var(--fg, #333);
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

.recurrence-info {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
  padding: 10px 12px;
  background: var(--muted-bg, #f9f9f9);
  border-radius: 8px;
  border: 1px solid var(--border, #e0e0e0);
}

.recurrence-icon {
  font-size: 1.2rem;
  flex-shrink: 0;
}

.recurrence-text {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--fg, #333);
  font-style: italic;
}

.occurrence-status {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
  padding: 10px 12px;
  background: var(--muted-bg, #f9f9f9);
  border-radius: 8px;
  border: 1px solid var(--border, #e0e0e0);
}

.status-label {
  font-weight: 600;
  color: var(--muted, #666);
  font-size: 0.9rem;
}

.status-value {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--fg, #333);
  padding: 2px 8px;
  border-radius: 4px;
  background: var(--muted-bg, #e8f5e9);
}

.participants-info {
  margin-left: auto;
  font-size: 0.9rem;
  color: var(--muted, #666);
  font-weight: 500;
}

.archived-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  background: #fef3c7;
  color: #92400e;
  border: 1px solid #fbbf24;
}
</style>
