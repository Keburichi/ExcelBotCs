<script setup lang="ts">
import type { FCEvent } from '@/features/events/events.types'
import { ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseCard from '@/components/BaseCard.vue'
import BaseModal from '@/components/BaseModal.vue'
import DiscordMessageRenderer from '@/components/DiscordMessageRenderer.vue'
import EventOrganizationDialog from '@/components/events/EventOrganizationDialog.vue'
import EventSignupDialog from '@/components/events/EventSignupDialog.vue'
import { useEvents } from '@/composables/useEvents'

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
</template>

<style scoped>

</style>
