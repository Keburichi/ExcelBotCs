<script setup lang="ts">
import type { FcMember } from '@/features/fcMembers/fcMembers.types'
import BaseCard from '@/components/BaseCard.vue'

const props = defineProps<{
  member: FcMember
  isMember?: boolean
}>()
</script>

<template>
  <BaseCard
    :title="props.member.Name" :subtitle="props.member.Title == '' ? '<none>' : props.member.Title"
    variant="elevated"
  >
    <template #avatar>
      <img
        v-if="props.member.Avatar" :src="props.member.Avatar" :alt="props.member.Name"
        class="card__avatar"
        referrerpolicy="no-referrer"
      >
      <span v-else class="avatar card">?</span>
    </template>
    <template #body>
      <p>{{ props.member.Bio }}</p>
    </template>
    <template #footer>
      <p>Rank: {{ props.member.FcRank }}</p>
      <a href="">lodestone</a>
      <a href="">fflogs</a>
      <a href="">tomestone</a>
    </template>
    <slot :member="member" />
  </BaseCard>
</template>

<style scoped>

</style>
