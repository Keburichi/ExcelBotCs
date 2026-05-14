<script setup lang="ts">
import type { GuessInfo } from '@/features/lottery/lottery.types'

defineProps<{
  guesses: GuessInfo[]
}>()
</script>

<template>
  <div>
    <h3 class="guesses-title">
      All Guesses
    </h3>
    <div v-if="guesses.length === 0" class="no-guesses">
      No guesses yet. Be the first!
    </div>
    <div v-else class="guesses-list">
      <div
        v-for="guess in guesses"
        :key="guess.Number"
        class="guess-row"
      >
        <span class="guess-num">{{ guess.Number }}</span>
        <span class="guess-names">{{ guess.Guessers.map(u => u.DiscordName).join(', ') }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.guesses-title {
  margin: 0 0 0.75rem 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
}

.no-guesses {
  padding: 1.5rem 0;
  color: var(--muted);
  font-size: 0.875rem;
}

.guesses-list {
  display: flex;
  flex-direction: column;
  max-height: 28rem;
  overflow-y: auto;
}

.guess-row {
  display: flex;
  gap: 0.75rem;
  align-items: baseline;
  padding: 0.375rem 0;
  border-bottom: 1px solid var(--border);
  font-size: 0.875rem;
}

.guess-row:last-child {
  border-bottom: none;
}

.guess-num {
  font-weight: 600;
  color: var(--link);
  min-width: 2rem;
  text-align: right;
  flex-shrink: 0;
}

.guess-names {
  color: var(--fg);
  line-height: 1.4;
}
</style>
