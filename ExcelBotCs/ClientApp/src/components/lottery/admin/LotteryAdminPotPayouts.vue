<script setup lang="ts">
import { computed, reactive } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { formatGil, formatInstant, formatRoundKey } from '@/features/lottery/lottery.format'
import { useLotteryAdmin } from '@/composables/useLotteryAdmin'
import LotteryPayouts from './LotteryPayouts.vue'
import LotteryPrizePool from './LotteryPrizePool.vue'

const admin = useLotteryAdmin()

const status = admin.status
const ledger = admin.ledger
const busy = admin.busy
const fieldErrors = admin.fieldErrors

const draft = reactive({
  Amount: 1_000_000,
  Direction: 'increase' as 'increase' | 'decrease',
  Reason: '',
})

const pot = computed(() => status.value?.Pot ?? 0)
const floorNote = computed(() => {
  const amount = Number(draft.Amount)
  if (!Number.isFinite(amount) || amount <= 0)
    return ''

  const signed = draft.Direction === 'increase' ? amount : -amount
  const next = pot.value + signed
  if (next >= 1_000_000)
    return `New pot: ${formatGil(next)} Gil.`

  return `Clamped at the 1,000,000 Gil floor: the correction applies ${formatGil(Math.max(0, signed + (next - 1_000_000)))} Gil of the requested ${formatGil(amount)}.`
})

async function adjust() {
  const amount = Number(draft.Amount)
  if (!Number.isFinite(amount) || amount <= 0) {
    fieldErrors.value = { ...fieldErrors.value, Amount: 'must be a positive whole number of Gil.' }
    return
  }

  if (draft.Reason.trim().length === 0) {
    fieldErrors.value = { ...fieldErrors.value, Reason: 'is required for the audit trail.' }
    return
  }

  const ok = await admin.adjustPot(Math.round(amount), draft.Direction, draft.Reason.trim())
  if (ok)
    draft.Reason = ''
}
</script>

<template>
  <div class="pot-payouts">
    <section class="card" aria-label="Pot adjustment">
      <div class="card__head">
        <h2 class="card__title">
          Pot
        </h2>
        <span class="pot">{{ formatGil(pot) }} Gil</span>
      </div>
      <p class="hint">
        Corrections are ledgered and audited, and can never take the pot below 1,000,000 Gil. The weekly increment and
        the base pot are deployment config, not editable here.
      </p>

      <form class="adjust" @submit.prevent="adjust">
        <div class="field">
          <label for="pot-direction">Direction</label>
          <select id="pot-direction" v-model="draft.Direction">
            <option value="increase">
              Increase
            </option>
            <option value="decrease">
              Decrease
            </option>
          </select>
        </div>

        <div class="field">
          <label for="pot-amount">Amount (Gil)</label>
          <input id="pot-amount" v-model.number="draft.Amount" min="1" step="100000" type="number">
          <span v-if="fieldErrors.Amount" class="field__error">{{ fieldErrors.Amount }}</span>
        </div>

        <div class="field field--wide">
          <label for="pot-reason">Reason</label>
          <input id="pot-reason" v-model="draft.Reason" placeholder="e.g. Riddle top-up, donation" type="text">
          <span v-if="fieldErrors.Reason" class="field__error">{{ fieldErrors.Reason }}</span>
        </div>

        <div class="adjust__submit">
          <BaseButton
            state="primary"
            size="small"
            title="Apply adjustment"
            :disabled="busy"
            @clicked="adjust"
          />
        </div>
      </form>
      <p v-if="floorNote" class="hint">
        {{ floorNote }}
      </p>
    </section>

    <section class="card" aria-label="Recent ledger">
      <h2 class="card__title">
        Recent ledger
      </h2>
      <p class="hint">
        Newest first. Every entry has a balance-after and an idempotency key, so a retry cannot double-count.
      </p>

      <p v-if="ledger.length === 0" class="empty">
        No ledger entries yet.
      </p>

      <div v-else class="table-wrap">
        <table class="ledger">
          <thead>
            <tr>
              <th scope="col">When</th>
              <th scope="col">Type</th>
              <th scope="col">Round</th>
              <th scope="col" class="num">Amount</th>
              <th scope="col" class="num">Balance</th>
              <th scope="col">Reason</th>
              <th scope="col">Actor</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="entry in ledger" :key="entry.Id">
              <td class="nowrap">
                {{ formatInstant(entry.CreatedAtUtc) }}
              </td>
              <td>{{ entry.Type }}</td>
              <td class="nowrap">
                {{ formatRoundKey(entry.RoundKey) }}
              </td>
              <td class="num" :class="{ 'num--negative': entry.Amount < 0 }">
                {{ entry.Amount > 0 ? '+' : '' }}{{ formatGil(entry.Amount) }}
              </td>
              <td class="num">
                {{ formatGil(entry.BalanceAfter) }}
              </td>
              <td>{{ entry.Reason }}</td>
              <td class="mono">
                {{ entry.ActorDiscordId ?? '—' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <BaseButton
        v-if="admin.hasMoreLedger.value"
        state="tertiary"
        size="small"
        title="Load more entries"
        :disabled="busy"
        @clicked="admin.loadMoreLedger()"
      />
    </section>

    <LotteryPrizePool />
    <LotteryPayouts />
  </div>
</template>

<style scoped>
.pot-payouts {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.card {
  padding: 1.25rem;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--card);
}

.card__head {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.75rem;
}

.card__title {
  margin: 0 0 0.75rem 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
}

.pot {
  color: var(--fg);
  font-size: 1.35rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.hint {
  margin: 0.5rem 0 0.75rem 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.5;
}

.adjust {
  display: grid;
  grid-template-columns: 10rem 12rem 1fr auto;
  gap: 0.75rem;
  align-items: end;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.field label {
  font-size: 0.78rem;
  font-weight: 500;
  color: var(--fg);
}

.field input,
.field select {
  padding: 0.45rem 0.625rem;
  border: 1px solid var(--border);
  border-radius: 9px;
  background: var(--bg);
  color: var(--fg);
  font-size: 0.85rem;
  font-variant-numeric: tabular-nums;
}

.field input:focus-visible,
.field select:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.field__error {
  color: var(--msg-error-fg);
  font-size: 0.72rem;
}

.adjust__submit {
  padding-bottom: 0.1rem;
}

.table-wrap {
  overflow-x: auto;
}

.ledger {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.78rem;
}

.ledger th {
  padding: 0.4rem 0.5rem;
  border-bottom: 1px solid var(--border);
  color: var(--muted);
  font-size: 0.68rem;
  font-weight: 600;
  text-align: left;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  white-space: nowrap;
}

.ledger td {
  padding: 0.4rem 0.5rem;
  border-bottom: 1px solid var(--border);
  color: var(--fg);
  vertical-align: top;
}

.ledger tr:last-child td {
  border-bottom: none;
}

.num {
  text-align: right;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.num--negative {
  color: var(--msg-error-fg);
}

.nowrap {
  white-space: nowrap;
}

.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.72rem;
}

.empty {
  margin: 0 0 0.75rem 0;
  color: var(--muted);
  font-size: 0.85rem;
}

@media (max-width: 900px) {
  .adjust {
    grid-template-columns: 1fr 1fr;
  }

  .field--wide {
    grid-column: 1 / -1;
  }
}

@media (max-width: 600px) {
  .adjust {
    grid-template-columns: 1fr;
  }
}
</style>
