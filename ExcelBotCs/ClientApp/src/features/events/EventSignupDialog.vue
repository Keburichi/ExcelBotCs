<script setup lang="ts">
import BaseModal from "@/components/BaseModal.vue";
import {FCEvent, Role} from "@/features/events/events.types";

const props = defineProps<{
  modelValue: boolean
  event: FCEvent
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>();

function signUp(fcEvent: FCEvent, role: Role) {
  alert('Signed up for: ' + fcEvent.Name + ' - ' + role)
}

</script>

<template>
  <BaseModal :modelValue="props.modelValue" @update:modelValue="emit('update:modelValue', $event)"
             :title="event.Name + ' - signup'" :description="event.Description">
    <template #image>
      <img v-if="event.PictureUrl" :src="event.PictureUrl" alt="avatar" class="card__image">
    </template>

    <template #actions>
      <button class="btn" @click="signUp(event, Role.Tank)">Tank</button>
      <button class="btn" @click="signUp(event, Role.Healer)">Healer</button>
      <button class="btn" @click="signUp(event, Role.Melee)">Melee</button>
      <button class="btn" @click="signUp(event, Role.Caster)">Caster</button>
      <button class="btn" @click="signUp(event, Role.Ranged)">Ranged</button>
    </template>
  </BaseModal>
</template>

<style scoped>

</style>