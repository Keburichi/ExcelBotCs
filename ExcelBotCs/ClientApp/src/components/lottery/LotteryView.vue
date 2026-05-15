<script lang="ts" setup>
import { computed, onMounted } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { useAuth } from '@/composables/useAuth'
import { useLottery } from '@/composables/useLottery'
import LotteryAdmin from './LotteryAdmin.vue'
import LotteryGuesses from './LotteryGuesses.vue'
import LotteryNumberGrid from './LotteryNumberGrid.vue'

const { isAdmin } = useAuth()
const lottery = useLottery()

const remainingGuesses = computed(() =>
  Math.max(0, lottery.totalGuesses.value - lottery.usedGuesses.value),
)

onMounted(lottery.load)

function handleGuess(num: number) {
  lottery.guess(num)
}

function handleChange(oldNum: number, newNum: number) {
  lottery.changeGuess(oldNum, newNum)
}

function handleSelect(num: number) {
  lottery.selectNumber(num)
}
</script>

<template>
  <section class="lottery">
    <div class="lottery-intro">
      <p class="lottery-desc">
        Every member gets one free guess each week. Attend FC events to earn additional guesses.
        Pick a number between 1 and 99. The jackpot starts at 1,000,000 gil and grows by another
        1,000,000 each week nobody hits the winning number.
      </p>

      <div v-if="!lottery.loading.value" class="lottery-status">
        <span class="status-label">Your guesses</span>
        <template v-if="lottery.totalGuesses.value > 0">
          <span class="status-pips">
            <span
              v-for="i in lottery.totalGuesses.value"
              :key="i"
              class="pip"
              :class="i <= lottery.usedGuesses.value ? 'pip--used' : 'pip--available'"
            />
          </span>
          <span v-if="remainingGuesses > 0" class="status-count">
            {{ remainingGuesses }} of {{ lottery.totalGuesses.value }} remaining
          </span>
          <span v-else class="status-count">All guesses used</span>
        </template>
        <span v-else class="status-count">No guesses available</span>
      </div>
    </div>

    <div v-if="lottery.error.value && lottery.error.value.trim().length > 0" class="message message--error">
      {{ lottery.error.value }}
    </div>

    <div v-if="lottery.response.value && lottery.response.value.trim().length > 0" class="message message--info">
      {{ lottery.response.value }}
    </div>

    <div class="lottery-columns">
      <div class="lottery-primary">
        <LotteryNumberGrid
          :all-guesses="lottery.allGuesses.value"
          :my-guesses="lottery.myGuesses.value"
          :selected-number="lottery.selectedNumber.value"
          @change="handleChange"
          @guess="handleGuess"
          @select="handleSelect"
        />

        <div class="toolbar">
          <span class="toolbar-label">Quick Pick</span>
          <div class="toolbar-actions">
            <BaseButton
              title="Any 1–99"
              size="small"
              :disabled="lottery.loading.value"
              @clicked="lottery.quickPick('any')"
            />
            <BaseButton
              state="secondary"
              title="Available only"
              size="small"
              :disabled="lottery.loading.value"
              @clicked="lottery.quickPick('available')"
            />
            <BaseButton
              state="tertiary"
              title="Taken only"
              size="small"
              :disabled="lottery.loading.value"
              @clicked="lottery.quickPick('taken')"
            />
          </div>
        </div>
      </div>

      <div class="lottery-secondary">
        <LotteryGuesses :guesses="lottery.allGuesses.value" />

        <div v-if="isAdmin" class="admin-divider">
          <LotteryAdmin @refresh="lottery.load" />
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.lottery {
  max-width: 1100px;
  margin: 0 auto;
}

.lottery-intro {
  display: flex;
  align-items: stretch;
  gap: 2rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid var(--border);
}

.lottery-desc {
  margin: 0;
  color: var(--muted);
  font-size: 0.9rem;
  line-height: 1.6;
  max-width: 52ch;
}

.lottery-status {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 0.5rem;
  flex-shrink: 0;
  padding: 0.875rem 1.25rem;
  border-radius: 12px;
  background: color-mix(in oklab, var(--link) 6%, var(--card));
  border: 1px solid color-mix(in oklab, var(--link) 12%, var(--border));
}

.status-label {
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--muted);
}

.status-pips {
  display: flex;
  gap: 0.375rem;
}

.pip {
  width: 1.25rem;
  height: 1.25rem;
  border-radius: 6px;
  transition: background 200ms ease;
}

.pip--available {
  background: var(--link);
}

.pip--used {
  background: var(--border);
}

.status-count {
  font-size: 0.8rem;
  color: var(--muted);
}

.message {
  padding: 0.75rem 1rem;
  border-radius: 12px;
  margin-bottom: 1rem;
  font-size: 0.875rem;
}

.message--error {
  background: var(--msg-error-bg);
  color: var(--msg-error-fg);
  border: 1px solid var(--msg-error-border);
}

.message--success {
  background: var(--msg-success-bg);
  color: var(--msg-success-fg);
  border: 1px solid var(--msg-success-border);
}

.message--info {
  background: var(--msg-info-bg);
  color: var(--msg-info-fg);
  border: 1px solid var(--msg-info-border);
}

.lottery-columns {
  display: grid;
  grid-template-columns: 1fr 22rem;
  gap: 2.5rem;
  align-items: start;
}

.lottery-primary {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.toolbar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
  padding-top: 0.25rem;
  border-top: 1px solid var(--border);
}

.toolbar-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.toolbar-actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.lottery-secondary {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.admin-divider {
  padding-top: 2rem;
  border-top: 1px solid var(--border);
}

@media (max-width: 900px) {
  .lottery-intro {
    flex-direction: column;
    gap: 1rem;
  }

  .lottery-columns {
    grid-template-columns: 1fr;
    gap: 2rem;
  }
}
</style>
