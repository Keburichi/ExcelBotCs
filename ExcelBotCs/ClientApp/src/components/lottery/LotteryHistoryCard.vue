<script setup lang="ts">
import type { LotteryHistoryItem } from '@/features/lottery/lottery.types'
import { computed } from 'vue'
import { formatGil, initials } from '@/features/lottery/lottery.format'

const props = defineProps<{
  item: LotteryHistoryItem
}>()

const winnerInitials = computed(() => initials(props.item.DisplayName))
</script>

<template>
  <li class="winner-card">
    <div class="winner-card__date">
      <time :datetime="item.RoundKey" class="winner-card__date-value">{{ item.Date }}</time>
      <span class="winner-card__date-label">Draw</span>
    </div>

    <div class="winner-card__body">
      <div class="winner-card__identity">
        <img
          v-if="item.AvatarUrl"
          :src="item.AvatarUrl"
          :alt="`${item.DisplayName}'s avatar`"
          class="winner-card__avatar"
          loading="lazy"
          decoding="async"
          referrerpolicy="no-referrer"
        >
        <span v-else class="winner-card__avatar winner-card__avatar--initials" aria-hidden="true">
          {{ winnerInitials }}
        </span>

        <span class="winner-card__name">{{ item.DisplayName }}</span>
        <span class="winner-card__guess">Guess {{ item.Guess }}</span>
      </div>

      <div class="winner-card__prizes">
        <span class="winner-card__gil">{{ formatGil(item.GilPrize) }} Gil</span>
        <span
          v-for="prize in item.PrizeItems"
          :key="prize"
          class="winner-card__prize"
        >
          {{ prize }}
        </span>
      </div>
    </div>
  </li>
</template>

<style scoped>
.winner-card {
  display: grid;
  grid-template-columns: 5.5rem 1fr;
  gap: 1.25rem;
  align-items: center;
  padding: 1rem 1.25rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--card);
  transition: border-color 200ms ease, box-shadow 200ms ease;
}

.winner-card:hover {
  border-color: color-mix(in oklab, var(--link) 40%, var(--border));
  box-shadow: 0 6px 18px color-mix(in oklab, var(--link) 12%, transparent);
}

.winner-card__date {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  padding-right: 1.25rem;
  border-right: 1px solid var(--border);
}

.winner-card__date-value {
  color: var(--fg);
  font-size: 0.95rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.winner-card__date-label {
  color: var(--muted);
  font-size: 0.65rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.winner-card__body {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  flex-wrap: wrap;
  min-width: 0;
}

.winner-card__identity {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  min-width: 0;
}

.winner-card__avatar {
  flex-shrink: 0;
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  object-fit: cover;
  border: 1px solid var(--border);
}

.winner-card__avatar--initials {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
  color: #fff;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}

.winner-card__name {
  color: var(--fg);
  font-size: 0.95rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.winner-card__guess {
  flex-shrink: 0;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  border: 1px solid color-mix(in oklab, var(--link) 30%, var(--border));
  background: color-mix(in oklab, var(--link) 10%, var(--card));
  color: var(--link);
  font-size: 0.75rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.winner-card__prizes {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  justify-content: flex-end;
}

.winner-card__gil {
  color: var(--fg);
  font-size: 0.9rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.winner-card__prize {
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
  border: 1px solid color-mix(in oklab, #f59e0b 35%, var(--border));
  background: color-mix(in oklab, #f59e0b 12%, var(--card));
  color: color-mix(in oklab, #b45309 80%, var(--fg));
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}

@media (max-width: 600px) {
  .winner-card {
    grid-template-columns: 1fr;
    gap: 0.625rem;
    padding: 0.875rem 1rem;
  }

  .winner-card__date {
    flex-direction: row;
    align-items: baseline;
    gap: 0.5rem;
    padding-right: 0;
    border-right: none;
  }

  .winner-card__body {
    align-items: flex-start;
    flex-direction: column;
    gap: 0.5rem;
  }

  .winner-card__prizes {
    justify-content: flex-start;
  }
}
</style>
