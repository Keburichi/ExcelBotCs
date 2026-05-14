<script lang="ts" setup>
import type { FcMember } from '@/features/fcMembers/fcMembers.types'
import { computed } from 'vue'
import BaseCard from '@/components/BaseCard.vue'

const props = defineProps<{
  member: FcMember
  isMember?: boolean
}>()

const rankBadgeClass = computed(() => {
  if (!props.member.FcRank) return ''
  const rank = props.member.FcRank.toLowerCase()
  if (rank.includes('master')) return 'rank-master'
  if (rank.includes('living memory')) return 'rank-living-memory'
  if (rank.includes('leader')) return 'rank-leader'
  if (rank.includes('officer')) return 'rank-officer'
  if (rank.includes('member')) return 'rank-member'
  return ''
})
</script>

<template>
  <BaseCard
    :subtitle="props.member.Title || undefined"
    :title="props.member.Name"
    title-class="card__title--lg"
    variant="elevated"
  >
    <template #avatar>
      <img
        v-if="props.member.Avatar"
        :alt="props.member.Name"
        :src="props.member.Avatar"
        class="card__avatar"
        referrerpolicy="no-referrer"
      >
      <span v-else class="card__avatar--placeholder" />
    </template>
    <template #body>
      <p v-if="props.member.Bio" class="member-bio">{{ props.member.Bio }}</p>
    </template>
    <template #footer>
      <span :class="rankBadgeClass" class="member-rank-badge">{{ props.member.FcRank }}</span>
    </template>
    <slot :member="member" />
  </BaseCard>
</template>

<style scoped>
.member-bio {
  color: var(--muted);
  font-size: 0.875rem;
  line-height: 1.5;
}

.member-rank-badge {
  display: inline-block;
  padding: 0.1875rem 0.625rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  background: var(--muted-bg);
  color: var(--muted);
}

.member-rank-badge.rank-master {
  background: var(--cat-indigo-bg);
  color: var(--cat-indigo-fg);
}

.member-rank-badge.rank-living-memory {
  background: var(--cat-teal-bg);
  color: var(--cat-teal-fg);
}

.member-rank-badge.rank-leader {
  background: var(--cat-amber-bg);
  color: var(--cat-amber-fg);
}

.member-rank-badge.rank-officer {
  background: var(--cat-green-bg);
  color: var(--cat-green-fg);
}

.member-rank-badge.rank-member {
  background: var(--cat-blue-bg);
  color: var(--cat-blue-fg);
}
</style>
