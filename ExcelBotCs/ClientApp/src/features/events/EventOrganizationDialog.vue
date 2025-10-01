<script setup lang="ts">

import {FCEvent} from "@/features/events/events.types";
import BaseModal from "@/components/BaseModal.vue";

const props = defineProps<{
  modelValue: boolean
  event: FCEvent
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>();

</script>

<template>
  <BaseModal :modelValue="props.modelValue" @update:modelValue="emit('update:modelValue', $event)" :title="'Organize Event'">
    <template #image>
      <img v-if="event.PictureUrl" :src="event.PictureUrl" alt="avatar" class="card__image">
    </template>
    <template #body>
      <p>Select the members who should participate in the Event '<b>{{ event.Name }}</b>' and click '<b>Save</b>' once you are done.</p>
      <p>The bot will automatically post a new message in <b>#upcoming-roster</b>.</p>
    </template>
  <template #actions>
    <button class="btn" @click="emit('update:modelValue', false)">Cancel</button>
    <button class="btn primary" @click="emit('update:modelValue', false)">Save</button>
  </template>
  </BaseModal>
</template>

<style scoped>

</style>