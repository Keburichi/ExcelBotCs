<script setup lang="ts">
import type { LotteryPayout } from '@/features/lottery/lottery.admin.types'
import { computed, reactive } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { formatGil, formatInstant } from '@/features/lottery/lottery.format'
import { useLotteryAdmin } from '@/composables/useLotteryAdmin'

// The payout queue, newest draw first like the history timeline. The filter makes the pending work easy to isolate; a
// forfeit is confirmed because it hands pool prizes back and is final (spec §5.9 "no silent redo").
const admin = useLotteryAdmin()

const payoutPage = admin.payoutPage
const payoutFilter = admin.payoutFilter
const prizes = admin.prizes
const busy = admin.busy

const FILTERS = [
  { value: '', label: 'All' },
  { value: 'pending', label: 'Pending' },
  { value: 'paid', label: 'Paid' },
  { value: 'forfeited', label: 'Forfeited' },
] as const

const noteDrafts = reactive<Record<string, string>>({})

const items = computed<LotteryPayout[]>(() => payoutPage.value?.Items ?? [])

function noteFor(winner: LotteryPayout): string {
  return noteDrafts[winner.Id] ?? winner.PayoutNote ?? ''
}

async function markPaid(winner: LotteryPayout) {
  await admin.updatePayout(winner.RoundKey, winner.Id, 'paid', noteFor(winner).trim() || null)
}

async function forfeit(winner: LotteryPayout) {
  const note = noteFor(winner).trim()
  const message = [
    `Forfeit ${winner.DisplayName}'s payout?`,
    '',
    'Their Gil is not owed, and any prize-pool items assigned to them return to the pool. This is audited and final.',
    note ? `\nNote: ${note}` : '',
  ].join('\n')

  if (!window.confirm(message))
    return

  await admin.updatePayout(winner.RoundKey, winner.Id, 'forfeited', note || null)
}

async function assignPrize(winner: LotteryPayout, prizeId: string) {
  if (!prizeId)
    return

  await admin.assignPrize(prizeId, winner.RoundKey, winner.Id)
}

const inPool = computed(() => prizes.value?.Items.filter(item => item.Status === 'in-pool') ?? [])
</script>

<template>
  <section class="card" aria-label="Winner payouts">
    <div class="card__head">
      <h2 class="card__title">
        Winner payouts
      </h2>
      <div class="filters">
        <button
          v-for="filter in FILTERS"
          :key="filter.value || 'all'"
          :aria-pressed="payoutFilter === filter.value"
          :class="['filter', { 'filter--active': payoutFilter === filter.value }]"
          type="button"
          @click="admin.setPayoutFilter(filter.value)"
        >
          {{ filter.label }}
        </button>
      </div>
    </div>

    <p class="hint">
      {{ payoutPage?.PendingCount ?? 0 }} pending. A paid or forfeited row keeps its actor, time and note as the audit.
    </p>

    <p v-if="items.length === 0" class="empty">
      No winners in this view yet.
    </p>

    <ul v-else class="payouts">
      <li v-for="winner in items" :key="winner.Id" class="payout">
        <div class="payout__row">
          <div class="payout__who">
            <span class="payout__name">{{ winner.DisplayName }}</span>
            <span class="payout__meta">
              {{ winner.RoundKey }} · guess {{ winner.Guess }} · {{ formatGil(winner.GilPrize) }} Gil
            </span>
          </div>
          <span :class="['tag', `tag--${winner.PayoutStatus}`]">{{ winner.PayoutStatus }}</span>
        </div>

        <ul v-if="winner.Items.length" class="items">
          <li v-for="item in winner.Items" :key="item" class="item">
            {{ item }}
          </li>
        </ul>

        <p v-if="winner.PayoutAt" class="payout__audit">
          {{ formatInstant(winner.PayoutAt) }} · by <code>{{ winner.PayoutActorDiscordId ?? '—' }}</code>
          <template v-if="winner.PayoutNote">
            · “{{ winner.PayoutNote }}”
          </template>
        </p>

        <div v-if="winner.PayoutStatus !== 'forfeited'" class="payout__actions">
          <input
            :value="noteFor(winner)"
            :aria-label="`Note for ${winner.DisplayName}`"
            class="note"
            placeholder="Note (optional)"
            type="text"
            @input="noteDrafts[winner.Id] = ($event.target as HTMLInputElement).value"
          >
          <BaseButton
            v-if="winner.PayoutStatus === 'pending'"
            state="secondary"
            size="small"
            :disabled="busy"
            title="Mark paid"
            @clicked="markPaid(winner)"
          />
          <BaseButton
            state="danger"
            size="small"
            :disabled="busy"
            title="Forfeit"
            @clicked="forfeit(winner)"
          />
          <select
            v-if="winner.PayoutStatus === 'pending' && inPool.length > 0"
            :id="`prize-for-${winner.Id}`"
            class="prize-select"
            @change="assignPrize(winner, ($event.target as HTMLSelectElement).value); ($event.target as HTMLSelectElement).value = ''"
          >
            <option value="">
              Assign a prize…
            </option>
            <option v-for="prize in inPool" :key="prize.Id" :value="prize.Id">
              {{ prize.Name }}
            </option>
          </select>
        </div>
      </li>
    </ul>

    <BaseButton
      v-if="payoutPage?.NextCursor"
      state="tertiary"
      size="small"
      title="Load more"
      :disabled="busy"
      @clicked="admin.loadMorePayouts()"
    />
  </section>
</template>

<style scoped>
.card {
  padding: 1.25rem;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--card);
}

.card__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.card__title {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
}

.filters {
  display: flex;
  gap: 0.25rem;
  padding: 0.25rem;
  border: 1px solid var(--border);
  border-radius: 9px;
  background: var(--muted-bg);
}

.filter {
  padding: 0.25rem 0.625rem;
  border: none;
  border-radius: 7px;
  background: transparent;
  color: var(--muted);
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
}

.filter--active {
  color: var(--fg);
  background: var(--card);
}

.filter:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.hint {
  margin: 0.5rem 0 0.75rem 0;
  color: var(--muted);
  font-size: 0.8rem;
}

.payouts {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 0 0 0.75rem 0;
  padding: 0;
  list-style: none;
}

.payout {
  padding: 0.75rem;
  border: 1px solid var(--border);
  border-radius: 11px;
}

.payout__row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.payout__who {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.payout__name {
  color: var(--fg);
  font-size: 0.9rem;
  font-weight: 600;
}

.payout__meta {
  color: var(--muted);
  font-size: 0.75rem;
}

.tag {
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.tag--pending {
  background: var(--msg-warning-bg, var(--muted-bg));
  color: var(--msg-warning-fg, var(--fg));
  border: 1px solid var(--msg-warning-border, var(--border));
}

.tag--paid {
  background: var(--msg-success-bg);
  color: var(--msg-success-fg);
  border: 1px solid var(--msg-success-border);
}

.tag--forfeited {
  background: var(--muted-bg);
  color: var(--muted);
  border: 1px solid var(--border);
}

.items {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem;
  margin: 0.5rem 0 0 0;
  padding: 0;
  list-style: none;
}

.item {
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
  background: color-mix(in oklab, var(--link) 12%, var(--card));
  border: 1px solid color-mix(in oklab, var(--link) 20%, var(--border));
  color: var(--fg);
  font-size: 0.72rem;
}

.payout__audit {
  margin: 0.5rem 0 0 0;
  color: var(--muted);
  font-size: 0.72rem;
  line-height: 1.5;
}

.payout__audit code {
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
}

.payout__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  margin-top: 0.625rem;
}

.note {
  flex: 1 1 12rem;
  min-width: 8rem;
  padding: 0.4rem 0.625rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--fg);
  font-size: 0.8rem;
}

.note:focus-visible,
.prize-select:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.prize-select {
  padding: 0.4rem 0.5rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--fg);
  font-size: 0.8rem;
}

.empty {
  margin: 0 0 0.75rem 0;
  color: var(--muted);
  font-size: 0.85rem;
}
</style>
