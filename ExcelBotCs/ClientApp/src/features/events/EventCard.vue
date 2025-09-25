<script setup lang="ts">

import {FCEvent} from "@/features/events/events.types";
import BaseCard from "@/components/BaseCard.vue";

const props = defineProps<{
  event: FCEvent
  isMember?: boolean
  isAdmin?: boolean
}>()

</script>

<template>

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
    </template>
    <!--    <template #actions>-->
    <!--      <button class="btn" v-if="props.isMember">Join</button>-->
    <!--      <button class="btn" v-else>View</button>-->
    <!--      <button class="btn" v-if="props.isAdmin">Edit</button>-->
    <!--      <button class="btn" v-else>View</button>-->
    <!--      <button class="btn" v-if="props.isAdmin">Delete</button>-->
    <!--      <button class="btn" v-else>View</button>-->
    <!--    </template>-->
  </BaseCard>

</template>

<style scoped>

</style>