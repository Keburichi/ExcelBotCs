<script lang="ts" setup>
import { onMounted } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { useAuth } from '@/composables/useAuth'
import { useLottery } from '@/composables/useLottery'
import LotteryAdmin from './LotteryAdmin.vue'
import LotteryGuesses from './LotteryGuesses.vue'
import LotteryNumberGrid from './LotteryNumberGrid.vue'

const { isAdmin } = useAuth()
const lottery = useLottery()

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
    <h2 class="lottery-title">
      Lottery
    </h2>

    <div v-if="lottery.error.value && lottery.error.value.trim().length > 0" class="message message--error">
      {{ lottery.error.value }}
    </div>

    <div v-if="lottery.response.value && lottery.response.value.trim().length > 0" class="message message--success">
      {{ lottery.response.value }}
    </div>

    <div class="lottery-columns">
      <div class="lottery-primary">
        <div class="status-bar">
          <p class="status-text">{{ lottery.view.value }}</p>
          <p v-if="lottery.selectedNumber.value" class="status-selection">
            Selected: <strong>{{ lottery.selectedNumber.value }}</strong>
          </p>
        </div>

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

        <LotteryNumberGrid
          :all-guesses="lottery.allGuesses.value"
          :my-guesses="lottery.myGuesses.value"
          :selected-number="lottery.selectedNumber.value"
          @change="handleChange"
          @guess="handleGuess"
          @select="handleSelect"
        />
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

.lottery-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 1.5rem 0;
  color: var(--fg);
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.02em;
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

.lottery-columns {
  display: grid;
  grid-template-columns: 1fr 22rem;
  gap: 2.5rem;
  align-items: start;
}

.lottery-primary {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.status-bar {
  display: flex;
  align-items: baseline;
  gap: 1rem;
  flex-wrap: wrap;
}

.status-text {
  margin: 0;
  color: var(--fg);
  line-height: 1.5;
  font-size: 0.9rem;
}

.status-selection {
  margin: 0;
  font-size: 0.85rem;
  color: var(--link);
}

.toolbar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.toolbar-label {
  font-size: 0.85rem;
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
  .lottery-columns {
    grid-template-columns: 1fr;
    gap: 2rem;
  }
}
</style>
