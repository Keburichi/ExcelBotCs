<script setup lang="ts">
import type { Member } from '@/features/members/members.types'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import BaseCard from '@/components/BaseCard.vue'

const props = defineProps<{
  member: Member
  isMember?: boolean
}>()

const router = useRouter()
const isEditOpen = ref(false)

function goEdit(member: Member) {
  router.push({ name: 'member-edit', params: { id: member.Id } })
}
</script>

<template>
  <!--  <MemberEditDialog v-model="isEditOpen" :member="props.member" @update:model-value="handleMemberEdit" /> -->

  <BaseCard :title="props.member.PlayerName" :subtitle="props.member.DiscordName" variant="elevated">
    <template #avatar>
      <img
        v-if="props.member.DiscordAvatar" :src="props.member.DiscordAvatar" :alt="props.member.PlayerName"
        class="card__avatar"
        referrerpolicy="no-referrer"
      >
      <span v-else class="avatar card">?</span>
    </template>
    <template #body>
      <input :id="props.member.DiscordId" v-model="props.member.Subbed" :name="props.member.DiscordId" type="checkbox" placeholder="Is player subbed?">
      <label :for="props.member.DiscordId">Subbed?</label>
      <!--      <p>Lodestone: {{ props.member.Subbed }}</p> -->
      <!--      <p>FFLogs: </p> -->
      <!--      <p>Tomestone: </p> -->
      <!--      <p>Hello Body World</p> -->
    </template>

    <template #actions>
      <BaseButton title="Edit" size="small" tooltip="Edit this member" @clicked="goEdit(member)" />
    </template>

    <template #footer>
      <a href="">lodestone</a>
      <a href="">fflogs</a>
      <a href="">tomestone</a>
    </template>
    <slot :member="member" />
  </BaseCard>
</template>

<style scoped>

</style>
