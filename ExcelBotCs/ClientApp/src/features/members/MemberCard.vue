<script setup lang="ts">

import {Member} from "@/features/members/members.types";
import BaseCard from "@/components/BaseCard.vue";

const props = defineProps<{
  member: Member
  isMember?: boolean
}>()

</script>

<template>

  <BaseCard :title="props.member.PlayerName" :subtitle="props.member.DiscordName" :variant="'elevated'">
    <template #avatar>
      <img v-if="props.member.DiscordAvatar" :src="props.member.DiscordAvatar" :alt="props.member.PlayerName"
           class="card__avatar"
           referrerpolicy="no-referrer"/>
      <span v-else class="avatar card">?</span>
    </template>
    <template #body>
      <p>Lodestone: </p>
      <p>FFLogs: </p>
      <p>Tomestone: </p>
      <p>Hello Body World</p>
    </template>
    <template #footer>
      <a href="">lodestone</a>
      <a href="">fflogs</a>
      <a href="">tomestone</a>
      <p>Hello footer World</p>
    </template>
    <slot :member="member"/>
  </BaseCard>
</template>

<style scoped>

</style>