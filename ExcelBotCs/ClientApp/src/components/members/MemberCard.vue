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
        <span v-if="props.member.Subbed" class="sub-badge sub-badge--active">Subscribed</span>
        <span v-else class="sub-badge sub-badge--inactive">Unsubscribed</span>
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

.sub-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.75rem;
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.sub-badge--active {
  background: rgba(34, 197, 94, 0.15);
  color: rgb(var(--color-success));
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.sub-badge--inactive {
  background: rgba(239, 68, 68, 0.15);
  color: var(--danger);
  border: 1px solid rgba(239, 68, 68, 0.3);
}

:root[data-theme='dark'] .sub-badge--active {
  background: rgba(34, 197, 94, 0.2);
  color: rgb(var(--color-success));
  border-color: rgba(34, 197, 94, 0.4);
}

:root[data-theme='dark'] .sub-badge--inactive {
  background: rgba(239, 68, 68, 0.2);
  color: var(--danger);
  border-color: rgba(239, 68, 68, 0.4);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .sub-badge--active {
    background: rgba(34, 197, 94, 0.2);
    color: rgb(var(--color-success));
    border-color: rgba(34, 197, 94, 0.4);
  }

  :root:not([data-theme='light']) .sub-badge--inactive {
    background: rgba(239, 68, 68, 0.2);
    color: var(--danger);
    border-color: rgba(239, 68, 68, 0.4);
  }
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
  color: var(--muted);
  font-size: 0.875rem;
  font-style: italic;
}
</style>
