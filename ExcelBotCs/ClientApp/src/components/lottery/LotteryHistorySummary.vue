<script setup lang="ts">
import type { LotteryHistoryTotals, LotteryLastDraw } from '@/features/lottery/lottery.types'
import { computed } from 'vue'
import { formatGil } from '@/features/lottery/lottery.format'

const props = defineProps<{
  totals: LotteryHistoryTotals
  lastDraw: LotteryLastDraw | null
}>()

/** The two winners of a shared draw are both named; more than that collapses to a count. */
const lastDrawNames = computed(() => {
  if (!props.lastDraw || props.lastDraw.winners.length === 0)
    return 'No draw yet'

  const names = props.lastDraw.winners.map(winner => winner.DisplayName)
  return names.length <= 2 ? names.join(' & ') : `${names.length} winners`
})

const lastDrawPrize = computed(() => {
  if (!props.lastDraw)
    return ''
  return `${formatGil(props.lastDraw.combinedGil)} Gil awarded`
})

const drawCountLabel = computed(() =>
  props.totals.Draws === 1 ? 'across 1 draw' : `across ${props.totals.Draws} draws`,
)
</script>

<template>
  <section class="history-summary" aria-label="Lottery summary">
    <article class="summary-card">
      <h2 class="summary-card__label">
        Current pot
      </h2>
      <p class="summary-card__value">
        {{ formatGil(totals.CurrentPot) }} Gil
      </p>
      <p class="summary-card__hint">
        What the next draw is playing for
      </p>
    </article>

    <article class="summary-card">
      <h2 class="summary-card__label">
        Last draw
      </h2>
      <p class="summary-card__value">
        {{ lastDraw ? lastDraw.date : '—' }}
      </p>
      <p class="summary-card__hint">
        <span>{{ lastDrawNames }}</span>
        <span v-if="lastDrawPrize" class="summary-card__hint-sep">·</span>
        <span v-if="lastDrawPrize">{{ lastDrawPrize }}</span>
      </p>
    </article>

    <article class="summary-card">
      <h2 class="summary-card__label">
        Winners recorded
      </h2>
      <p class="summary-card__value">
        {{ totals.Winners }}
      </p>
      <p class="summary-card__hint">
        {{ drawCountLabel }}
      </p>
    </article>
  </section>
</template>

<style scoped>
.history-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.summary-card {
  padding: 1rem 1.25rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--card);
  transition: border-color 200ms ease, box-shadow 200ms ease;
}

.summary-card:hover {
  border-color: color-mix(in oklab, var(--link) 40%, var(--border));
  box-shadow: 0 6px 18px color-mix(in oklab, var(--link) 12%, transparent);
}

.summary-card__label {
  margin: 0 0 0.35rem 0;
  color: var(--muted);
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.summary-card__value {
  margin: 0;
  color: var(--fg);
  font-size: 1.35rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  line-height: 1.2;
}

.summary-card__hint {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0.35rem;
  margin: 0.4rem 0 0 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.4;
}

.summary-card__hint-sep {
  color: var(--border);
}

@media (max-width: 900px) {
  .history-summary {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 600px) {
  .history-summary {
    grid-template-columns: 1fr;
    gap: 0.75rem;
  }
}
</style>
