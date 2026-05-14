<script lang="ts" setup>
import type { GuessInfo } from '@/features/lottery/lottery.types'
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
  if (isMyGuess(num)) {
    emit('select', num)
    return
  }

  if (props.selectedNumber !== null) {
    if (!isGuessed(num)) {
      emit('change', props.selectedNumber, num)
    }
    return
  }

  if (props.myGuesses.length === 0 && !isGuessed(num)) {
    emit('guess', num)
    return
  }

  emit('select', num)
}

function handleGuessClick(num: number) {
  if (isMyGuess(num)) {
    return
  }

  if (props.myGuesses.length > 0 && props.selectedNumber === null) {
    emit('guess', num)
    return
  }

  if (props.selectedNumber !== null) {
    emit('change', props.selectedNumber, num)
    return
  }

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
  <div class="grid-wrap">
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
        <div class="legend-dot legend-dot--available" />
        <span>Available</span>
      </div>
      <div class="legend-item">
        <div class="legend-dot legend-dot--my-guess" />
        <span>Your Guess</span>
      </div>
      <div class="legend-item">
        <div class="legend-dot legend-dot--selected" />
        <span>Selected</span>
      </div>
      <div class="legend-item">
        <div class="legend-dot legend-dot--taken" />
        <span>Taken</span>
      </div>
    </div>
    <p class="grid-help">
      Click your guess to select it, then click an available number to reassign.
    </p>
  </div>
</template>

<style scoped>
.grid-wrap {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.number-grid {
  display: grid;
  grid-template-columns: repeat(10, 1fr);
  gap: 0.375rem;
}

@media (max-width: 640px) {
  .number-grid {
    grid-template-columns: repeat(5, 1fr);
  }
}

.grid-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  padding-top: 0.25rem;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.8rem;
  color: var(--muted);
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}

.legend-dot--available {
  background: var(--lot-available);
}

.legend-dot--my-guess {
  background: var(--lot-mine);
}

.legend-dot--selected {
  background: var(--lot-selected);
}

.legend-dot--taken {
  background: var(--lot-taken);
}

.grid-help {
  color: var(--muted);
  font-size: 0.8rem;
  margin: 0;
}
</style>
