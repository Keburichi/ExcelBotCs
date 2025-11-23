<script setup lang="ts">
import type { GuessInfo } from '@/features/lottery/lottery.types'

defineProps<{
  guesses: GuessInfo[]
}>()
</script>

<template>
  <div class="guesses-container">
    <h3 class="guesses-title">
      All Current Guesses
    </h3>
    <div v-if="guesses.length === 0" class="no-guesses">
      No guesses yet. Be the first to make a guess!
    </div>
    <div v-else class="guesses-table-container">
      <table class="guesses-table">
        <thead>
          <tr>
            <th>Number</th>
            <th>Guessed By</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="guess in guesses" :key="guess.Number">
            <td class="number-cell">
              {{ guess.Number }}
            </td>
            <td>{{ guess.Guessers.map(u => u.DiscordName).join(', ') }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.guesses-container {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 16px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  padding: 1.5rem;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

:root[data-theme='dark'] .guesses-container {
  background: rgba(18, 26, 45, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .guesses-container {
    background: rgba(18, 26, 45, 0.7);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.guesses-title {
  margin: 0 0 1rem 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--fg, #111827);
}

.no-guesses {
  text-align: center;
  padding: 2rem;
  color: var(--muted, #6b7280);
  font-style: italic;
}

.guesses-table-container {
  overflow-x: auto;
  border: 1px solid var(--border, #e5e7eb);
  border-radius: 16px;
}

.guesses-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.guesses-table thead {
  background: var(--card, #fff);
  border-bottom: 2px solid var(--border, #e5e7eb);
}

.guesses-table th {
  padding: 0.75rem;
  text-align: left;
  font-weight: 600;
  color: var(--fg, #111827);
  border-bottom: 2px solid var(--border, #e5e7eb);
  white-space: nowrap;
}

.guesses-table td {
  padding: 0.75rem;
  border-bottom: 1px solid var(--border, #e5e7eb);
  color: var(--fg, #111827);
}

.guesses-table tbody tr:last-child td {
  border-bottom: none;
}

.guesses-table tbody tr:hover {
  background: rgba(0, 0, 0, 0.02);
}

[data-theme="dark"] .guesses-table tbody tr:hover {
  background: rgba(255, 255, 255, 0.05);
}

.number-cell {
  font-weight: 600;
  color: #3b82f6;
}
</style>
