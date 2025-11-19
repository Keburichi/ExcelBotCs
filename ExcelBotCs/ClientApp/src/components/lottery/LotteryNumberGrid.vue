<script lang="ts" setup>
import type {GuessInfo} from '@/features/lottery/lottery.types'
import BaseButton from '@/components/BaseButton.vue'

const props = defineProps<{
  allGuesses: GuessInfo[]
  myGuesses: number[]
  selectedNumber: number | null
}>()

const emit = defineEmits<{
  select: [number: number]
  guess: [number: number]
  change: [oldNumber: number, newNumber: number]
}>()

function isMyGuess(num: number): boolean {
  return props.myGuesses.includes(num)
}

function isGuessed(num: number): boolean {
  return props.allGuesses.some(g => g.Number === num)
}

function handleNumberClick(num: number) {
  // If user clicks on their own guess, select it for reassignment
  if (isMyGuess(num)) {
    emit('select', num)
    return
  }

  // If a guess is selected (an existing guess to change) and the target is available, perform change
  if (props.selectedNumber !== null) {
    if (!isGuessed(num)) {
      emit('change', props.selectedNumber, num)
    }
    // If target is taken, ignore single click (user can try another available number)
    return
  }

  // If no selection yet and user can add a guess (no guesses made) and target is available, make a guess
  if (props.myGuesses.length === 0 && !isGuessed(num)) {
    emit('guess', num)
    return
  }

  // Otherwise just toggle selection state on available numbers
  emit('select', num)
}

function handleGuessClick(num: number) {
  if (isMyGuess(num)) {
    // Can't guess a number you already have
    return
  }

  if (props.myGuesses.length > 0 && props.selectedNumber === null) {
    // If user has guesses but hasn't selected one to change, just add a new guess
    emit('guess', num)
    return
  }

  if (props.selectedNumber !== null) {
    // Change guess
    emit('change', props.selectedNumber, num)
    return
  }

  // First guess
  emit('guess', num)
}

function getButtonState(num: number): 'primary' | 'pressed' | 'tertiary' | 'danger' {
  if (num === props.selectedNumber)
    return 'pressed'

  if (isMyGuess(num))
    return 'tertiary'

  if (isGuessed(num))
    return 'danger'

  return 'primary'
}

function getTooltip(num: number): string | undefined {
  const guess = props.allGuesses.find(g => g.Number === num)
  if (guess && guess.Guessers.length > 0) {
    return guess.Guessers.map(u => u.DiscordName).join(', ')
  }
  return undefined
}
</script>

<template>
  <div class="number-grid-container">
    <div class="number-grid">
      <BaseButton
          v-for="num in 100"
          :key="num"
          :state="getButtonState(num)"
          :title="num.toString()"
          :tooltip="getTooltip(num)"
          size="small"
          @clicked="handleNumberClick(num)"
          @dblclick="handleGuessClick(num)"
      />
    </div>
    <div class="grid-legend">
      <div class="legend-item">
        <div class="legend-box legend-box--available"/>
        <span>Available</span>
      </div>
      <div class="legend-item">
        <div class="legend-box legend-box--my-guess"/>
        <span>Your Guess</span>
      </div>
      <div class="legend-item">
        <div class="legend-box legend-box--selected"/>
        <span>Selected</span>
      </div>
      <div class="legend-item">
        <div class="legend-box legend-box--taken"/>
        <span>Taken</span>
      </div>
    </div>
    <p class="grid-help">
      Click your existing guess, then click an available number to reassign. On first pick, click an available number to
      guess.
    </p>
  </div>
</template>

<style scoped>
.number-grid-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.number-grid {
  display: grid;
  grid-template-columns: repeat(10, 1fr);
  gap: 0.5rem;
}

@media (max-width: 768px) {
  .number-grid {
    grid-template-columns: repeat(5, 1fr);
  }
}

.grid-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 1rem;
  background: rgba(0, 0, 0, 0.02);
  border-radius: 0.5rem;
  border: 1px solid var(--border, #e5e7eb);
}

[data-theme="dark"] .grid-legend {
  background: rgba(255, 255, 255, 0.05);
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.legend-box {
  width: 24px;
  height: 24px;
  border-radius: 0.375rem;
  border: 2px solid;
}

.legend-box--available {
  background: #3b82f6;
  border-color: #2563eb;
}

.legend-box--my-guess {
  background: #8b5cf6;
  border-color: #7c3aed;
}

.legend-box--selected {
  background: #10b981;
  border-color: #059669;
}

.legend-box--taken {
  background: #ef4444;
  border-color: #dc2626;
}

.grid-help {
  text-align: center;
  color: var(--muted, #6b7280);
  font-size: 0.875rem;
  margin: 0;
}
</style>
