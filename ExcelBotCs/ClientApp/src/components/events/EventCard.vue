<script lang="ts" setup>
import type { EventOccurrence, FCEvent } from '@/features/events/events.types'
import type { Fight } from '@/features/fights/fights.types'
import { computed, onMounted, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
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

const fights = ref<Fight[]>([])

const nextOccurrence = computed((): EventOccurrence | null => {
  return getNextOccurrence(fcEventValue.value)
})

const occurrenceToComplete = computed((): EventOccurrence | null => {
  return getOccurrenceToComplete(fcEventValue.value)
})

const pastScheduledOccurrence = computed((): EventOccurrence | null => {
  if (!fcEventValue.value.Occurrences) return null
  const now = new Date()
  return fcEventValue.value.Occurrences
    .filter(o =>
      o.Status === OccurrenceStatus.Scheduled
      && new Date(o.OccurrenceDate) < now,
    )
    .sort((a, b) => new Date(a.OccurrenceDate).getTime() - new Date(b.OccurrenceDate).getTime())[0] || null
})

const eventTypeLabel = computed(() => {
  return fcEventValue.value.Type !== undefined ? eventTypeToString(fcEventValue.value.Type) : null
})

const associatedFight = computed(() => {
  if (!fcEventValue.value.FightId || fights.value.length === 0) return null
  return fights.value.find(f => f.Id === fcEventValue.value.FightId)
})

const formattedDuration = computed(() => {
  const minutes = fcEventValue.value.Duration
  if (minutes < 60) return `${minutes} min`
  const hours = Math.floor(minutes / 60)
  const remainingMinutes = minutes % 60
  if (remainingMinutes === 0) return `${hours}h`
  return `${hours}h ${remainingMinutes}min`
})

const formattedDate = computed(() => {
  const occurrenceDate = nextOccurrence.value?.OccurrenceDate ?? fcEventValue.value.StartDate
  const startDate = new Date(occurrenceDate)
  return startDate.toLocaleString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  })
})

const formattedTimeRange = computed(() => {
  const occurrenceDate = nextOccurrence.value?.OccurrenceDate ?? fcEventValue.value.StartDate
  const startDate = new Date(occurrenceDate)
  const endDate = new Date(startDate.getTime() + fcEventValue.value.Duration * 60 * 1000)

  const startTime = startDate.toLocaleString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })
  const endTime = endDate.toLocaleString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })

  return `${startTime} - ${endTime}`
})

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

  return `${startTime} - ${endTime} ST`
})

const eventIsRecurring = computed(() => {
  return fcEventValue.value.ICalString && isRecurring(fcEventValue.value.ICalString)
})

const recurrenceDescription = computed(() => {
  if (!eventIsRecurring.value) return ''
  const config = parseICalString(fcEventValue.value.ICalString)
  return config ? describeRecurrence(config, fcEventValue.value.ICalString) : ''
})

async function handleSignupDialogClose(value: boolean) {
  isOpen.value = value
  if (!value) {
    const updatedEvent = await getEvent(fcEventValue.value.Id)
    if (updatedEvent) fcEventValue.value = updatedEvent
  }
}

function getSignUpNumber(fcEvent: FCEvent) {
  if (!fcEvent.Signups) return 0
  return fcEvent.Signups.filter(signup => signup.Roles.length > 0).length
}

function getParticipantCount(fcEvent: FCEvent) {
  if (!fcEvent.Groups || fcEvent.Groups.length === 0) return 0
  return fcEvent.Groups.flatMap(g => g.Participants).length
}

function openFightResources() {
  isRaidplanDialogOpen.value = true
}

async function handleEventConcluded() {
  const updatedEvent = await getEvent(fcEventValue.value.Id)
  if (updatedEvent) {
    fcEventValue.value = updatedEvent
    if (updatedEvent.IsArchived) emit('archived', updatedEvent)
  }
}

async function skipPastOccurrence() {
  if (!pastScheduledOccurrence.value) return
  try {
    await updateOccurrenceStatusById(
      fcEventValue.value.Id,
      pastScheduledOccurrence.value.Id,
      OccurrenceStatus.Cancelled,
    )
    const updatedEvent = await getEvent(fcEventValue.value.Id)
    if (updatedEvent) {
      fcEventValue.value = updatedEvent
      if (updatedEvent.IsArchived) emit('archived', updatedEvent)
    }
  }
  catch (error) {
    console.error('Error skipping occurrence:', error)
  }
}

function handleExtended(updatedEvent: FCEvent) {
  fcEventValue.value = updatedEvent
  emit('extended', updatedEvent)
}

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
  <!-- Dialogs -->
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
  <RaidplanDialog
    v-if="associatedFight"
    v-model:is-open="isRaidplanDialogOpen"
    :fight="associatedFight"
    @close="isRaidplanDialogOpen = false"
  />

  <!-- Card -->
  <article class="event-card">
    <!-- Image banner -->
    <div v-if="fcEventValue.PictureUrl" class="event-card__image-wrap">
      <img
        :src="fcEventValue.PictureUrl"
        :alt="fcEventValue.Name"
        class="event-card__image"
        referrerpolicy="no-referrer"
      >
      <div class="event-card__image-fade" />
    </div>

    <div class="event-card__content">
      <!-- Header: title + badges -->
      <div class="event-card__header">
        <h3 class="event-card__title">{{ fcEventValue.Name }}</h3>
        <div class="event-card__badges">
          <span v-if="fcEventValue.IsArchived" class="event-badge event-badge--archived">Archived</span>
          <span
            v-if="eventTypeLabel"
            :class="`event-badge--${eventTypeLabel.toLowerCase()}`"
            class="event-badge"
          >{{ eventTypeLabel }}</span>
        </div>
      </div>

      <!-- Time block -->
      <div class="event-card__time">
        <div class="event-card__time-primary">
          <span class="event-card__date">{{ formattedDate }}</span>
          <span class="event-card__time-range">{{ formattedTimeRange }}</span>
        </div>
        <div class="event-card__time-secondary">
          <span class="event-card__server-time">{{ serverTimeRange }}</span>
          <span class="event-card__duration">{{ formattedDuration }}</span>
        </div>
      </div>

      <!-- Recurrence -->
      <div v-if="eventIsRecurring" class="event-card__recurrence">
        {{ recurrenceDescription }}
      </div>

      <!-- Description -->
      <div v-if="fcEventValue.Description" class="event-card__description">
        <DiscordMessageRenderer :content="fcEventValue.Description" />
      </div>

      <!-- Metadata row -->
      <div class="event-card__meta">
        <span v-if="nextOccurrence" class="event-card__status">
          {{ occurrenceStatusToString(nextOccurrence.Status) }}
        </span>
        <span v-if="nextOccurrence" class="event-card__participants">
          {{ getParticipantCount(fcEventValue) }}/{{ fcEventValue.MaxNumberOfParticipants }} participants
        </span>
        <span class="event-card__organizer">Organized by {{ fcEventValue.Organizer }}</span>
        <span v-if="associatedFight" class="event-card__fight">
          <BaseButton
            :title="associatedFight.Name"
            :tooltip="`View ${associatedFight.Name} resources`"
            variant="text"
            size="small"
            @clicked="openFightResources"
          />
        </span>
      </div>

      <!-- Actions -->
      <div v-if="!props.isArchiveView" class="event-card__actions">
        <BaseButton
          :disabled="!props.isMember || !fcEventValue.AvailableForSignup"
          :title="`Sign up (${getSignUpNumber(fcEventValue)})`"
          size="small"
          tooltip="Sign up for this event"
          @clicked="isOpen = true"
        />
        <BaseButton
          v-if="props.isAdmin && fcEventValue.AvailableForSignup && nextOccurrence"
          size="small"
          state="secondary"
          title="Select Participants"
          @clicked="isOrganizationOpen = true"
        />
        <BaseButton
          v-if="props.isAdmin && pastScheduledOccurrence"
          :tooltip="`Skip occurrence from ${new Date(pastScheduledOccurrence.OccurrenceDate).toLocaleDateString()}`"
          size="small"
          state="secondary"
          title="Skip Past"
          @clicked="skipPastOccurrence"
        />
        <BaseButton
          v-if="props.isAdmin && occurrenceToComplete && occurrenceToComplete.Status !== OccurrenceStatus.Completed && occurrenceToComplete.Status !== OccurrenceStatus.Cancelled"
          size="small"
          title="Conclude"
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
          v-if="props.isAdmin"
          size="small"
          title="Edit"
          tooltip="Edit event"
          @clicked="emit('startEdit', fcEventValue)"
        />
        <BaseButton
          v-if="props.isAdmin"
          size="small"
          state="danger"
          title="Delete"
          @clicked="isDeleteOpen = true"
        />
      </div>
    </div>
  </article>
</template>

<style scoped>
.event-card {
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

:root[data-theme='dark'] .event-card {
  background: rgba(18, 26, 45, 0.7);
  border-color: rgba(255, 255, 255, 0.15);
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .event-card {
    background: rgba(18, 26, 45, 0.7);
    border-color: rgba(255, 255, 255, 0.15);
    box-shadow:
      0 4px 16px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

/* Image banner */
.event-card__image-wrap {
  position: relative;
  overflow: hidden;
  max-height: 180px;
}

.event-card__image {
  width: 100%;
  height: 180px;
  object-fit: cover;
  display: block;
}

.event-card__image-fade {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 40px;
  background: linear-gradient(to top, rgba(255, 255, 255, 0.7), transparent);
  pointer-events: none;
}

:root[data-theme='dark'] .event-card__image-fade {
  background: linear-gradient(to top, rgba(18, 26, 45, 0.7), transparent);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .event-card__image-fade {
    background: linear-gradient(to top, rgba(18, 26, 45, 0.7), transparent);
  }
}

/* Content area */
.event-card__content {
  padding: 1rem 1.25rem 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  flex: 1;
}

/* Header */
.event-card__header {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0.5rem;
}

.event-card__title {
  font-size: 1.25rem;
  font-weight: 700;
  line-height: 1.3;
  margin: 0;
  flex: 1;
  min-width: 0;
}

.event-card__badges {
  display: flex;
  gap: 0.375rem;
  flex-shrink: 0;
}

/* Badges */
.event-badge {
  display: inline-block;
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
  font-size: 0.6875rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  background: var(--muted-bg);
  color: var(--muted);
}

.event-badge--archived {
  background: var(--cat-amber-bg);
  color: var(--cat-amber-fg);
}

.event-badge--raid { background: var(--cat-blue-bg); color: var(--cat-blue-fg); }
.event-badge--social { background: var(--cat-purple-bg); color: var(--cat-purple-fg); }
.event-badge--farming { background: var(--cat-green-bg); color: var(--cat-green-fg); }
.event-badge--maps { background: var(--cat-orange-bg); color: var(--cat-orange-fg); }
.event-badge--blu { background: var(--cat-teal-bg); color: var(--cat-teal-fg); }
.event-badge--academy { background: var(--cat-rose-bg); color: var(--cat-rose-fg); }
.event-badge--minilvl { background: var(--cat-amber-bg); color: var(--cat-amber-fg); }
.event-badge--downsynced { background: var(--cat-indigo-bg); color: var(--cat-indigo-fg); }
.event-badge--other { background: var(--cat-slate-bg); color: var(--cat-slate-fg); }

/* Time block */
.event-card__time {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.event-card__time-primary {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.event-card__time-secondary {
  display: flex;
  align-items: baseline;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.event-card__date {
  font-weight: 600;
  font-size: 0.9375rem;
}

.event-card__time-range {
  font-size: 0.9375rem;
  color: var(--fg);
}

.event-card__server-time {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--fg);
}

.event-card__duration {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--fg);
}

/* Recurrence */
.event-card__recurrence {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--muted);
  font-style: italic;
}

/* Description */
.event-card__description {
  font-size: 0.875rem;
  line-height: 1.6;
  color: var(--fg);
  border-top: 1px solid var(--border);
  padding-top: 0.75rem;
}

.event-card__description :deep(.discord-message__content) {
  white-space: normal;
}

.event-card__description :deep(.discord-headline) {
  font-size: 1.125rem;
  margin: 0.5rem 0 0.25rem;
}

/* Metadata row */
.event-card__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.25rem 0.75rem;
  font-size: 0.8125rem;
  color: var(--muted);
  margin-top: auto;
  padding-top: 0.5rem;
  border-top: 1px solid var(--border);
}

.event-card__status {
  font-weight: 600;
  color: var(--fg);
}

.event-card__participants {
  font-weight: 500;
}

.event-card__fight {
  margin-left: auto;
}

/* Actions */
.event-card__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  padding-top: 0.5rem;
}
</style>
