<script setup lang="ts">

import {FCEvent} from "@/features/events/events.types";
import BaseCard from "@/components/BaseCard.vue";
import BaseModal from "@/components/BaseModal.vue";
import {ref} from "vue";
import EventSignupDialog from "@/features/events/EventSignupDialog.vue";
import EventOrganizationDialog from "@/features/events/EventOrganizationDialog.vue";

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

function signUp(fcEvent: FCEvent) {
  isOpen.value = true
}

function getSignUpNumber(fcEvent: FCEvent) {
  if (!fcEvent.Signups) 
    return 0
  
  // count the number of signups where at least one role is selected
  return fcEvent.Signups.filter(signup => signup.Roles.length > 0).length;
}

</script>

<template>  
  <EventSignupDialog v-model="isOpen" :event="props.event" @update:modelValue=""/>
  
  <EventOrganizationDialog v-model="isOrganizationOpen" :event="props.event" />
  
  <BaseModal v-model="isDeleteOpen" :title="'Deleting Event - ' + event.Name">
    <template #body>
      <p>Are you sure you want to delete this event?</p>
    </template>
    <template #actions>
      <button class="btn" @click="isDeleteOpen = false">Cancel</button>
      <button class="btn danger" @click="emit('delete-event', props.event)">Yes, delete this</button>
    </template>
  </BaseModal>

  <BaseCard :title="props.event.Name" :size="'large'" :variant="'elevated'">
    <template #image>
      <img v-if="props.event.PictureUrl" :src="props.event.PictureUrl" alt="avatar" class="card__image"
           referrerpolicy="no-referrer"/>
      <span v-else class="card__image placeholder">?</span>
    </template>
    <template #body>
      <p>{{ props.event.Description }}</p>
    </template>
    <template #footer>
      <p>Organized by: {{ props.event.Organizer }}</p>
      <div class="actions">
        <button v-if="isMember" class="btn primary actions" @click="signUp(props.event)">Sign up ({{getSignUpNumber(event)}})</button>
        <button v-if="isAdmin" class="btn secondary actions" @click="isOrganizationOpen=true">Select participants</button>
        <button v-if="isAdmin" class="btn danger actions" @click="isDeleteOpen=true">Delete</button>
      </div>
    </template>
    <template #actions>
      <button v-if="isAdmin" class="btn" @click="emit('start-edit', props.event)">Edit</button>
    </template>
  </BaseCard>

</template>

<style scoped>

</style>