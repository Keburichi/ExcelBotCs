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
  <section class="lottery-view">
    <div class="page-header">
      <h2 class="page-title">
        Lottery
      </h2>
    </div>

    <div v-if="lottery.error.value && lottery.error.value.trim().length > 0" class="message message--error">
      {{ lottery.error.value }}
    </div>

    <div v-if="lottery.response.value && lottery.response.value.trim().length > 0" class="message message--success">
      {{ lottery.response.value }}
    </div>

    <div class="lottery-info">
      <div class="info-card">
        <h3 class="info-title">
          Your Status
        </h3>
        <p class="info-content">
          {{ lottery.view.value }}
        </p>
        <p v-if="lottery.selectedNumber.value" class="info-hint">
          Selected: <strong>{{ lottery.selectedNumber.value }}</strong>
          <br>
          Click your existing guess to select it, then click an available number to change.
        </p>
      </div>
    </div>

    <div class="quick-pick-section">
      <h3 class="section-title">
        Quick Pick
      </h3>
      <div class="quick-pick-actions">
        <BaseButton
          title="Any 1–99"
          :disabled="lottery.loading.value"
          @clicked="lottery.quickPick('any')"
        />
        <BaseButton
          state="secondary"
          title="Available only"
          :disabled="lottery.loading.value"
          @clicked="lottery.quickPick('available')"
        />
        <BaseButton
          state="tertiary"
          title="Taken only"
          :disabled="lottery.loading.value"
          @clicked="lottery.quickPick('taken')"
        />
      </div>
      <p class="quick-pick-hint">
        Tip: If you have selected your current guess, Quick Pick will reassign it to a random number.
      </p>
    </div>

    <div class="lottery-grid-section">
      <h3 class="section-title">
        Select Your Numbers
      </h3>
      <LotteryNumberGrid
        :all-guesses="lottery.allGuesses.value"
        :my-guesses="lottery.myGuesses.value"
        :selected-number="lottery.selectedNumber.value"
        @change="handleChange"
        @guess="handleGuess"
        @select="handleSelect"
      />
    </div>

    <div class="lottery-guesses-section">
      <LotteryGuesses :guesses="lottery.allGuesses.value" />
    </div>

    <div v-if="isAdmin" class="lottery-admin-section">
      <LotteryAdmin @refresh="lottery.load" />
    </div>
  </section>
</template>

<style scoped>
.lottery-view {
  max-width: 1400px;
  margin: 0 auto;
  padding: 1.5rem;
}

/* Page header */
.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  color: var(--fg);
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.02em;
}

.message {
  padding: 1rem;
  border-radius: 0.5rem;
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

.lottery-info {
  margin-bottom: 2rem;
}

.info-card {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 16px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  padding: 1.5rem;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

:root[data-theme='dark'] .info-card {
  background: rgba(18, 26, 45, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .info-card {
    background: rgba(18, 26, 45, 0.7);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.info-card:hover {
  backdrop-filter: blur(24px);
  border-color: rgba(59, 130, 246, 0.3);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.15),
  0 4px 16px rgba(0, 0, 0, 0.1),
  inset 0 1px 0 rgba(255, 255, 255, 0.6);
}

:root[data-theme='dark'] .info-card:hover {
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
  0 4px 16px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .info-card:hover {
    border-color: rgba(59, 130, 246, 0.4);
    box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
}

.info-title {
  margin: 0 0 1rem 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--fg);
}

.info-content {
  margin: 0;
  color: var(--fg);
  line-height: 1.6;
}

.info-hint {
  margin: 1rem 0 0 0;
  padding: 0.75rem;
  border: 1px solid var(--link);
  background: color-mix(in oklab, var(--card) 95%, var(--link) 5%);
  border-radius: 8px;
  color: var(--fg);
  font-size: 0.875rem;
}

.lottery-grid-section,
.lottery-guesses-section,
.lottery-admin-section {
  margin-bottom: 2rem;
}

.section-title {
  font-size: 1.5rem;
  font-weight: 600;
  margin: 0 0 1rem 0;
  color: var(--fg);
}

.quick-pick-section {
  margin-bottom: 2rem;
}

.quick-pick-actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.quick-pick-hint {
  margin: 0.5rem 0 0 0;
  color: var(--muted);
  font-size: 0.875rem;
}
</style>
