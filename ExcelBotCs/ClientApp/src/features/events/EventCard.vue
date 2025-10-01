<script setup lang="ts">

import {FCEvent} from "@/features/events/events.types";
import BaseCard from "@/components/BaseCard.vue";
import BaseModal from "@/components/BaseModal.vue";
import {ref} from "vue";
import EventSignupDialog from "@/features/events/EventSignupDialog.vue";

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

function signUp(fcEvent: FCEvent) {
  isOpen.value = true
}

const isOpen = ref(false)

</script>

<template>  
  <EventSignupDialog v-model="isOpen" :event="props.event"/>

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
      <button v-if="isMember" class="btn primary" @click="signUp(props.event)">Sign up</button>
    </template>
    <template #actions>
      <button v-if="isAdmin" class="btn" @click="emit('start-edit', props.event)">Edit</button>
    </template>
  </BaseCard>

</template>

<style scoped>

</style>