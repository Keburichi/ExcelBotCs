<script lang="ts" setup>
import type { FcMember } from '@/features/fcMembers/fcMembers.types'
import { computed } from 'vue'
import BaseCard from '@/components/BaseCard.vue'

const props = defineProps<{
  member: FcMember
  isMember?: boolean
}>()

const rankBadgeClass = computed(() => {
  if (!props.member.FcRank) {
    return ''
  }
  const rank = props.member.FcRank.toLowerCase()
  if (rank.includes('master')) {
    return 'rank-master'
  }
  else if (rank.includes('living memory')) {
    return 'rank-living-memory'
  }
  else if (rank.includes('leader')) {
    return 'rank-leader'
  }
  else if (rank.includes('officer')) {
    return 'rank-officer'
  }
  else if (rank.includes('member')) {
    return 'rank-member'
  }
  return ''
})
</script>

<template>
  <BaseCard
    :subtitle="props.member.Title == '' ? '<none>' : props.member.Title" :title="props.member.Name"
    title-class="text-2xl font-bold"
    variant="elevated"
  >
    <template #avatar>
      <img
        v-if="props.member.Avatar" :alt="props.member.Name" :src="props.member.Avatar"
        class="card__avatar"
        referrerpolicy="no-referrer"
      >
      <span v-else class="avatar card">?</span>
    </template>
    <template #body>
      <p>{{ props.member.Bio }}</p>
    </template>
    <template #footer>
      <div class="member-rank">
        <span>Rank:</span>
        <span :class="rankBadgeClass" class="member-rank-badge">{{ props.member.FcRank }}</span>
      </div>
    </template>
    <slot :member="member" />
  </BaseCard>
</template>

<style scoped>
.member-rank {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}

.member-rank-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 0.85rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  background: var(--muted-bg, #e0e0e0);
  color: var(--fg, #333);
}

/* Rank specific colors */
.member-rank-badge.rank-master {
  background: #ede7f6;
  color: #4527a0;
}

.member-rank-badge.rank-living-memory {
  background: #e0f2f1;
  color: #00695c;
}

.member-rank-badge.rank-leader {
  background: #ffecb3; /* Light amber */
  color: #ff6f00; /* Dark amber */
}

.member-rank-badge.rank-officer {
  background: #c8e6c9; /* Light green */
  color: #2e7d32; /* Dark green */
}

.member-rank-badge.rank-member {
  background: #e3f2fd; /* Light blue */
  color: #1565c0; /* Dark blue */
}
</style>
