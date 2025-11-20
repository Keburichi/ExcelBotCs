<script setup lang="ts">
import type { FCEvent } from '@/features/events/events.types'
import type { Fight } from '@/features/fights/fights.types'
import { computed, onMounted, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseCard from '@/components/BaseCard.vue'
import BaseModal from '@/components/BaseModal.vue'
import DiscordMessageRenderer from '@/components/DiscordMessageRenderer.vue'
import EventOrganizationDialog from '@/components/events/EventOrganizationDialog.vue'
import EventSignupDialog from '@/components/events/EventSignupDialog.vue'
import RaidplanDialog from '@/components/fights/RaidplanDialog.vue'
import { useEvents } from '@/composables/useEvents'
import { eventTypeToString } from '@/features/events/events.types'
import { FightsApi } from '@/features/fights/fights.api'

const props = defineProps<{
  isMember?: boolean
  isAdmin?: boolean
}>()

const emit = defineEmits<{
  startEdit: [event: FCEvent]
  cancelEdit: []
  saveEdit: []
  deleteEvent: [event: FCEvent]
  cardClick: [event: FCEvent]
}>()

const fcEventValue = defineModel<FCEvent>('fcEvent', { required: true })

const isOpen = ref(false)
const isOrganizationOpen = ref(false)
const isDeleteOpen = ref(false)
const isRaidplanDialogOpen = ref(false)

// Fights data for lookup
const fights = ref<Fight[]>([])

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

// Format date/time for display
const localDateTime = computed(() => {
  const date = new Date(fcEventValue.value.StartDate)
  return date.toLocaleString(undefined, {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
})

const serverDateTime = computed(() => {
  const date = new Date(fcEventValue.value.StartDate)
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

// Handle EventSignupDialog close - fetch updated event data
async function handleSignupDialogClose(value: boolean) {
  isOpen.value = value

  // When dialog closes (value becomes false), fetch updated event data
  if (!value) {
    const updatedEvent = await useEvents().getEvent(fcEventValue.value.Id)
    if (updatedEvent) {
      fcEventValue.value = updatedEvent
    }
  }
}

function getSignUpNumber(fcEvent: FCEvent) {
  if (!fcEvent.Signups)
    return 0

  // count the number of signups where at least one role is selected
  return fcEvent.Signups.filter(signup => signup.Roles.length > 0).length
}

function openFightResources(event: MouseEvent) {
  event.stopPropagation() // Prevent event card click
  isRaidplanDialogOpen.value = true
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
  <EventSignupDialog v-model="isOpen" :event="fcEventValue" @update:model-value="handleSignupDialogClose" />

  <EventOrganizationDialog
    v-model:is-open="isOrganizationOpen"
    v-model:fc-event="fcEventValue"
    @event-planned="handleSignupDialogClose(false)"
  />

  <BaseModal v-model="isDeleteOpen" :title="`Deleting Event - ${fcEventValue.Name}`">
    <template #body>
      <p>Are you sure you want to delete this event?</p>
    </template>
    <template #actions>
      <BaseButton title="Cancel" @clicked="isDeleteOpen = false" />
      <BaseButton title="Yes, delete this!" state="danger" @clicked="emit('deleteEvent', fcEventValue)" />
    </template>
  </BaseModal>

  <BaseCard :title="fcEventValue.Name" size="large" variant="elevated">
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
      <div v-if="eventTypeLabel || associatedFight" class="event-metadata">
        <span v-if="eventTypeLabel" :class="`type-${eventTypeLabel.toLowerCase()}`" class="event-type-badge">
          {{ eventTypeLabel }}
        </span>
        <span v-if="associatedFight" class="fight-info">
          <strong>Fight:</strong>
          <button
            :title="`View ${associatedFight.Name} resources`"
            class="fight-link"
            @click="openFightResources"
          >
            {{ associatedFight.Name }}
          </button>
        </span>
      </div>
      <div class="event-datetime">
        <div class="datetime-row">
          <span class="datetime-label">Local Time:</span>
          <span class="datetime-value">{{ localDateTime }}</span>
        </div>
        <div class="datetime-row">
          <span class="datetime-label">Server Time:</span>
          <span class="datetime-value">{{ serverDateTime }} (ST)</span>
        </div>
      </div>
      <p>Organized by: {{ fcEventValue.Organizer }}</p>
      <div class="actions">
        <BaseButton
          :title="`Sign up (${getSignUpNumber(fcEventValue)})`"
          :disabled="!props.isMember || !fcEventValue.AvailableForSignup"
          tooltip="Sign up for this event"
          size="small"
          @clicked="isOpen = true"
        />
        <BaseButton v-if="props.isAdmin && fcEventValue.AvailableForSignup" title="Select Participants" size="small" state="secondary" @clicked="isOrganizationOpen = true" />
        <BaseButton v-if="props.isAdmin && !fcEventValue.AvailableForSignup" title="Conclude Event" size="small" tooltip="Conclude Event" />
        <BaseButton v-if="props.isAdmin" title="Delete" size="small" state="danger" @clicked="isDeleteOpen = true" />
      </div>
    </template>
    <template #actions>
      <BaseButton v-if="props.isAdmin" title="Edit" size="medium" tooltip="Edit event" @clicked="emit('startEdit', fcEventValue)" />
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

.fight-link {
  background: none;
  border: none;
  padding: 0;
  font: inherit;
  color: var(--link, #2563eb);
  cursor: pointer;
  text-decoration: none;
  transition: all 0.2s;
  font-weight: 500;
}

.fight-link:hover {
  text-decoration: underline;
  color: var(--link-hover, #1d4ed8);
}

.fight-link:focus {
  outline: 2px solid var(--link, #2563eb);
  outline-offset: 2px;
  border-radius: 2px;
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
</style>
