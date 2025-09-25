<script setup lang="ts">

import {Member} from "@/features/members/members.types";
import BaseCard from "@/components/BaseCard.vue";

const props = defineProps<{
  member: Member
  isMember?: boolean
}>()

</script>

<template>

  <BaseCard :title="props.member.PlayerName" :size="'small'" :variant="'elevated'">
    <template #avatar>
      <img v-if="props.member.DiscordAvatar" :src="props.member.DiscordAvatar" alt="avatar" class="card__avatar"
           referrerpolicy="no-referrer"/>
      <span v-else class="avatar placeholder">?</span>
    </template>
    <slot :member="member"/>
  </BaseCard>

  <!--<div id="members-container">-->
  <!--  <div v-for="m in props.items" :key="m.Id" class="member-card">-->
  <!--    <img v-if="m.DiscordAvatar" :src="m.DiscordAvatar" alt="avatar" class="avatar" referrerpolicy="no-referrer"/>-->
  <!--    <span v-else class="avatar placeholder">?</span>-->
  <!--    <div class="member-card-content">-->
  <!--      <h3><b>{{ m.PlayerName }}</b></h3>-->
  <!--    </div>-->
  <!--  </div>-->
  <!--</div>-->
</template>

<style scoped>

</style>