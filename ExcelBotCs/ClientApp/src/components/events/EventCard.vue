<script setup lang="ts">
import type { FCEvent } from '@/features/events/events.types'
import { ref } from 'vue'
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

  <EventOrganizationDialog v-model:is-open="isOrganizationOpen" v-model:event="fcEventValue" />

  <BaseModal v-model="isDeleteOpen" :title="`Deleting Event - ${fcEventValue.Name}`">
    <template #body>
      <p>Are you sure you want to delete this event?</p>
    </template>
    <template #actions>
      <button class="btn" @click="isDeleteOpen = false">
        Cancel
      </button>
      <button class="btn danger" @click="emit('deleteEvent', fcEventValue)">
        Yes, delete this!
      </button>
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
        <button v-if="props.isMember" class="btn actions" @click="isOpen = true">
          Sign up ({{ getSignUpNumber(fcEventValue) }})
        </button>
        <button v-if="props.isAdmin" class="btn secondary actions" @click="isOrganizationOpen = true">
          Select participants
        </button>
        <button v-if="props.isAdmin" class="btn danger actions" @click="isDeleteOpen = true">
          Delete
        </button>
      </div>
    </template>
    <template #actions>
      <button v-if="props.isAdmin" class="btn" @click="emit('startEdit', fcEventValue)">
        Edit
      </button>
    </template>
  </BaseCard>
</template>

<style scoped>

</style>
