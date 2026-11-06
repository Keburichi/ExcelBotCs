<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { useLotteryAdmin } from '@/composables/useLotteryAdmin'

// The non-Gil prize pool (spec §5.5): items are indivisible and stay in the pool until assigned to a winner. The
// assignment is the one write that also appends the item to the winner's history row.
const admin = useLotteryAdmin()

const prizes = admin.prizes
const payoutPage = admin.payoutPage
const busy = admin.busy

const newPrize = ref('')
const assigning = reactive<Record<string, string>>({})

const inPool = computed(() => prizes.value?.Items.filter(item => item.Status === 'in-pool') ?? [])
const awarded = computed(() => prizes.value?.Items.filter(item => item.Status === 'awarded') ?? [])
const pendingWinners = computed(() => (payoutPage.value?.Items ?? []).filter(item => item.PayoutStatus === 'pending'))

async function addPrize() {
  const name = newPrize.value.trim()
  if (!name)
    return

  const ok = await admin.addPrize(name)
  if (ok)
    newPrize.value = ''
}

async function removePrize(id: string, name: string) {
  if (!window.confirm(`Remove “${name}” from the prize pool? It is not awarded to anyone.`))
    return

  await admin.removePrize(id)
}

async function assign(prizeId: string) {
  const winnerId = assigning[prizeId]
  if (!winnerId)
    return

  const winner = pendingWinners.value.find(item => item.Id === winnerId)
  if (!winner)
    return

  const ok = await admin.assignPrize(prizeId, winner.RoundKey, winner.Id)
  if (ok)
    delete assigning[prizeId]
}
</script>

<template>
  <section class="card" aria-label="Prize pool">
    <div class="card__head">
      <h2 class="card__title">
        Prize pool
      </h2>
      <span class="count">{{ prizes?.InPoolCount ?? 0 }} in the pool · {{ prizes?.AwardedCount ?? 0 }} awarded</span>
    </div>
    <p class="hint">
      Non-Gil items. They do not change the pot. An item goes to exactly one winner; assigning it also puts it on that
      winner's history card.
    </p>

    <form class="add" @submit.prevent="addPrize">
      <label class="visually-hidden" for="new-prize">New prize name</label>
      <input
        id="new-prize"
        v-model="newPrize"
        class="add__input"
        placeholder="e.g. Chaotic Mount"
        type="text"
      >
      <BaseButton
        state="secondary"
        size="small"
        title="Add prize"
        :disabled="busy || newPrize.trim().length === 0"
        @clicked="addPrize"
      />
    </form>

    <p v-if="inPool.length === 0" class="empty">
      The pool is empty.
    </p>

    <ul v-else class="prizes">
      <li v-for="item in inPool" :key="item.Id" class="prize">
        <span class="prize__name">{{ item.Name }}</span>
        <div class="prize__actions">
          <select
            v-if="pendingWinners.length > 0"
            v-model="assigning[item.Id]"
            :aria-label="`Assign ${item.Name} to a winner`"
            class="prize__select"
          >
            <option value="">
              Assign to…
            </option>
            <option v-for="winner in pendingWinners" :key="winner.Id" :value="winner.Id">
              {{ winner.DisplayName }} ({{ winner.RoundKey }})
            </option>
          </select>
          <BaseButton
            v-if="assigning[item.Id]"
            state="primary"
            size="small"
            :disabled="busy"
            title="Assign"
            @clicked="assign(item.Id)"
          />
          <BaseButton
            state="tertiary"
            size="small"
            :disabled="busy"
            title="Remove"
            @clicked="removePrize(item.Id, item.Name)"
          />
        </div>
      </li>
    </ul>

    <p v-if="pendingWinners.length === 0 && inPool.length > 0" class="hint">
      There is no pending winner to assign an item to right now.
    </p>

    <details v-if="awarded.length > 0" class="awarded">
      <summary>Awarded items ({{ awarded.length }})</summary>
      <ul class="prizes">
        <li v-for="item in awarded" :key="item.Id" class="prize">
          <span class="prize__name">{{ item.Name }}</span>
          <span class="prize__awarded">
            {{ item.AwardedToName ?? item.AwardedToDiscordId }} · {{ item.AwardedRoundKey }}
          </span>
        </li>
      </ul>
    </details>
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

.count {
  color: var(--muted);
  font-size: 0.75rem;
  font-variant-numeric: tabular-nums;
}

.hint {
  margin: 0.5rem 0 0.75rem 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.5;
}

.add {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.add__input {
  flex: 1 1 12rem;
  padding: 0.45rem 0.625rem;
  border: 1px solid var(--border);
  border-radius: 9px;
  background: var(--bg);
  color: var(--fg);
  font-size: 0.85rem;
}

.add__input:focus-visible,
.prize__select:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.prizes {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.prize {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--border);
  border-radius: 10px;
}

.prize__name {
  color: var(--fg);
  font-size: 0.85rem;
  font-weight: 500;
}

.prize__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.375rem;
}

.prize__select {
  padding: 0.35rem 0.5rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--fg);
  font-size: 0.78rem;
}

.prize__awarded {
  color: var(--muted);
  font-size: 0.75rem;
}

.awarded {
  margin-top: 1rem;
}

.awarded summary {
  cursor: pointer;
  color: var(--muted);
  font-size: 0.8rem;
  font-weight: 600;
}

.awarded summary:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.awarded .prizes {
  margin-top: 0.5rem;
}

.empty {
  margin: 0;
  color: var(--muted);
  font-size: 0.85rem;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  margin: -1px;
  padding: 0;
  overflow: hidden;
  clip: rect(0 0 0 0);
  white-space: nowrap;
  border: 0;
}
</style>
