<script setup lang="ts">
import type { Member } from '@/features/members/members.types'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import BaseCard from '@/components/BaseCard.vue'
import ExperienceTags from './ExperienceTags.vue'

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

  <BaseCard
    :subtitle="props.member.DiscordName" :title="props.member.PlayerName" title-class="text-2xl font-bold"
    variant="elevated"
  >
    <template #avatar>
      <img
        v-if="props.member.DiscordAvatar" :src="props.member.DiscordAvatar" :alt="props.member.PlayerName"
        class="card__avatar"
        referrerpolicy="no-referrer"
      >
      <div v-else class="card__avatar card__avatar--placeholder" :title="`No avatar for ${props.member.PlayerName}`" />
    </template>
    <template #body>
      <div class="subbed-section">
        <input :id="props.member.DiscordId" v-model="props.member.Subbed" :name="props.member.DiscordId" type="checkbox" placeholder="Is player subbed?">
        <label :for="props.member.DiscordId">Subbed?</label>
      </div>
    </template>

    <template #actions>
      <BaseButton title="Edit" size="small" tooltip="Edit this member" @clicked="goEdit(member)" />
    </template>

    <template #footer>
      <div v-if="props.isMember" class="footer-content">
        <div v-if="props.member.Experience?.length" class="experience-footer">
          <ExperienceTags :experience="props.member.Experience" />
        </div>
        <div v-else class="no-experience-footer">
          <span class="muted">No cleared content yet</span>
        </div>
      </div>
      <div v-else class="footer-links">
        <a href="">lodestone</a>
        <a href="">fflogs</a>
        <a href="">tomestone</a>
      </div>
    </template>
    <slot :member="member" />
  </BaseCard>
</template>

<style scoped>
.subbed-section {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  justify-content: center;
}

.subbed-section input[type="checkbox"] {
  cursor: pointer;
  width: 1.25rem;
  height: 1.25rem;
}

.subbed-section label {
  cursor: pointer;
  margin: 0;
  font-weight: 500;
}

.footer-content {
  width: 100%;
}

.experience-footer {
  display: flex;
  justify-content: center;
}

.no-experience-footer {
  display: flex;
  justify-content: center;
}

.footer-links {
  display: flex;
  gap: 1rem;
  justify-content: center;
}

.muted {
  color: var(--muted, #6b7280);
  font-size: 0.875rem;
  font-style: italic;
}
</style>
