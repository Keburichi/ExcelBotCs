<script setup lang="ts">

import {FCEvent} from "@/features/events/events.types";
import BaseCard from "@/components/BaseCard.vue";
import BaseModal from "@/components/BaseModal.vue";
import DiscordMessageRenderer from "@/components/DiscordMessageRenderer.vue";
import {ref, watch} from "vue";
import EventSignupDialog from "@/features/events/EventSignupDialog.vue";
import EventOrganizationDialog from "@/features/events/EventOrganizationDialog.vue";
import {useEvents} from "@/features/events/useEvents";

const props = defineProps<{
  event: FCEvent
  isMember?: boolean
  isAdmin?: boolean
}>()

const emit = defineEmits<{
  'start-edit': [event: FCEvent]
  'cancel-edit': []
  'save-edit': [],
  'delete-event': [event: FCEvent],
  'card-click': [event: FCEvent]
}>()

const isOpen = ref(false)
const isOrganizationOpen = ref(false)
const isDeleteOpen = ref(false)

// Create a local reactive copy of the event to allow updates
const localEvent = ref<FCEvent>(props.event)

// Watch for prop changes to update local event
watch(() => props.event, (newEvent) => {
  localEvent.value = newEvent
}, { deep: true })

function signUp(fcEvent: FCEvent) {
  isOpen.value = true
}

// Handle EventSignupDialog close - fetch updated event data
async function handleSignupDialogClose(value: boolean) {
  isOpen.value = value
  
  // When dialog closes (value becomes false), fetch updated event data
  if (!value) {
    const updatedEvent = await useEvents().getEvent(props.event.Id)
    if (updatedEvent) {
      localEvent.value = updatedEvent
    }
  }
}

function getSignUpNumber(fcEvent: FCEvent) {
  if (!fcEvent.Signups) 
    return 0
  
  // count the number of signups where at least one role is selected
  return fcEvent.Signups.filter(signup => signup.Roles.length > 0).length;
}

</script>

<template>  
  <EventSignupDialog v-model="isOpen" :event="localEvent" @update:modelValue="handleSignupDialogClose"/>
  
  <EventOrganizationDialog v-model="isOrganizationOpen" :event="localEvent" />
  
  <BaseModal v-model="isDeleteOpen" :title="'Deleting Event - ' + localEvent.Name">
    <template #body>
      <p>Are you sure you want to delete this event?</p>
    </template>
    <template #actions>
      <button class="btn" @click="isDeleteOpen = false">Cancel</button>
      <button class="btn danger" @click="emit('delete-event', localEvent)">Yes, delete this!</button>
    </template>
  </BaseModal>

  <BaseCard :title="localEvent.Name" :size="'large'" :variant="'elevated'">
    <template #image>
      <img v-if="localEvent.PictureUrl" :src="localEvent.PictureUrl" alt="avatar" class="card__image"
           referrerpolicy="no-referrer"/>
      <span v-else class="card__image placeholder">?</span>
    </template>
    <template #body>
      <DiscordMessageRenderer :content="localEvent.Description"/>
    </template>
    <template #footer>
      <p>Organized by: {{ localEvent.Organizer }}</p>
      <div class="actions">
        <button v-if="isMember" class="btn actions" @click="signUp(localEvent)">Sign up ({{getSignUpNumber(localEvent)}})</button>
        <button v-if="isAdmin" class="btn secondary actions" @click="isOrganizationOpen=true">Select participants</button>
        <button v-if="isAdmin" class="btn danger actions" @click="isDeleteOpen=true">Delete</button>
      </div>
    </template>
    <template #actions>
      <button v-if="isAdmin" class="btn" @click="emit('start-edit', localEvent)">Edit</button>
    </template>
  </BaseCard>

</template>

<style scoped>

</style>