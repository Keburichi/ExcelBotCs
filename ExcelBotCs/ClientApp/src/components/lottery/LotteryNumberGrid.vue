<script lang="ts" setup>
import type { GuessInfo } from '@/features/lottery/lottery.types'
import { computed, ref } from 'vue'

const props = defineProps<{
  allGuesses: GuessInfo[]
  myGuesses: number[]
  selectedNumber: number | null
  maxNumber?: number
}>()

const emit = defineEmits<{
  select: [number: number]
  guess: [number: number]
  change: [oldNumber: number, newNumber: number]
}>()

const inputValue = ref('')
const inputError = ref('')

const max = computed(() => props.maxNumber ?? 99)

const takenSet = computed(() => new Set(props.allGuesses.map(g => g.Number)))

const guessOwners = computed(() => {
  const map = new Map<number, string[]>()
  for (const g of props.allGuesses) {
    map.set(g.Number, g.Guessers.map(u => u.DiscordName))
  }
  return map
})

function isMyGuess(num: number): boolean {
  return props.myGuesses.includes(num)
}

function handleInputSubmit() {
  inputError.value = ''
  const num = Number.parseInt(inputValue.value, 10)

  if (isNaN(num) || num < 1 || num > max.value) {
    inputError.value = `Enter a number between 1 and ${max.value}`
    return
  }

  if (props.selectedNumber !== null) {
    if (num === props.selectedNumber) {
      inputError.value = 'That is your currently selected guess'
      return
    }
    emit('change', props.selectedNumber, num)
  } else {
    if (isMyGuess(num)) {
      inputError.value = 'You already have this number'
      return
    }
    emit('guess', num)
  }

  inputValue.value = ''
}

function handleCellClick(num: number) {
  inputError.value = ''

  if (isMyGuess(num)) {
    emit('select', num)
    return
  }

  if (props.selectedNumber !== null) {
    emit('change', props.selectedNumber, num)
    return
  }

  emit('guess', num)
}

function handleChipClick(num: number) {
  inputError.value = ''
  emit('select', num)
}

function getCellState(num: number): 'mine' | 'mine-selected' | 'taken' | 'available' {
  if (num === props.selectedNumber) return 'mine-selected'
  if (isMyGuess(num)) return 'mine'
  if (takenSet.value.has(num)) return 'taken'
  return 'available'
}

function getCellTooltip(num: number): string | undefined {
  const owners = guessOwners.value.get(num)
  if (owners && owners.length > 0) return owners.join(', ')
  return undefined
}

const inputPlaceholder = computed(() => {
  if (props.selectedNumber !== null) {
    return `New number to replace #${props.selectedNumber}`
  }
  return `Pick a number (1–${max.value})`
})
</script>

<template>
  <div class="picker">
    <div class="input-row">
      <div class="input-wrap">
        <input
          v-model="inputValue"
          type="number"
          :min="1"
          :max="max"
          :placeholder="inputPlaceholder"
          class="number-input"
          @keydown.enter="handleInputSubmit"
        >
        <button
          class="input-submit"
          type="button"
          :disabled="!inputValue"
          @click="handleInputSubmit"
        >
          {{ selectedNumber !== null ? 'Reassign' : 'Guess' }}
        </button>
      </div>
      <p v-if="inputError" class="input-error">{{ inputError }}</p>
    </div>

    <div v-if="myGuesses.length > 0" class="my-guesses">
      <span class="my-guesses-label">Your guesses</span>
      <div class="guess-chips">
        <button
          v-for="num in myGuesses"
          :key="num"
          type="button"
          class="guess-chip"
          :class="{ 'guess-chip--active': num === selectedNumber }"
          @click="handleChipClick(num)"
        >
          {{ num }}
        </button>
      </div>
      <p v-if="selectedNumber !== null" class="reassign-hint">
        Type a new number above or click one on the map to reassign #{{ selectedNumber }}.
        <button type="button" class="cancel-link" @click="$emit('select', selectedNumber!)">Cancel</button>
      </p>
    </div>

    <div class="map">
      <div
        v-for="num in max"
        :key="num"
        class="cell"
        :class="`cell--${getCellState(num)}`"
        :data-tooltip="getCellTooltip(num)"
        @click="handleCellClick(num)"
      >
        {{ num }}
      </div>
    </div>

    <div class="map-legend">
      <div class="legend-item">
        <span class="legend-dot legend-dot--available" />
        <span>Available</span>
      </div>
      <div class="legend-item">
        <span class="legend-dot legend-dot--mine" />
        <span>Yours</span>
      </div>
      <div class="legend-item">
        <span class="legend-dot legend-dot--taken" />
        <span>Taken</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.picker {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

/* Input */
.input-row {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.input-wrap {
  display: flex;
  gap: 0.5rem;
}

.number-input {
  flex: 1;
  min-width: 0;
  font-size: 0.95rem;
  font-variant-numeric: tabular-nums;
  -moz-appearance: textfield;
}

.number-input::-webkit-inner-spin-button,
.number-input::-webkit-outer-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.input-submit {
  padding: 0.5rem 1rem;
  border-radius: 12px;
  font-weight: 600;
  font-size: 0.875rem;
  background: var(--link);
  color: var(--bg);
  border: none;
  cursor: pointer;
  transition: background 200ms ease;
  white-space: nowrap;
}

.input-submit:hover {
  background: var(--link-hover);
}

.input-submit:disabled {
  opacity: 0.4;
  cursor: default;
}

.input-submit:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.input-error {
  margin: 0;
  font-size: 0.8rem;
  color: var(--danger);
}

/* My guesses chips */
.my-guesses {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.my-guesses-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.guess-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
}

.guess-chip {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 2.25rem;
  height: 2rem;
  padding: 0 0.5rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  cursor: pointer;
  border: 2px solid var(--lot-mine-border);
  background: var(--lot-mine);
  color: var(--bg);
  transition: border-color 200ms ease, box-shadow 200ms ease;
}

.guess-chip:hover {
  box-shadow: 0 0 0 2px color-mix(in oklab, var(--lot-mine) 40%, transparent);
}

.guess-chip--active {
  border-color: var(--lot-selected-border);
  background: var(--lot-selected);
  box-shadow: 0 0 0 3px color-mix(in oklab, var(--lot-selected) 40%, transparent);
}

.guess-chip:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.reassign-hint {
  margin: 0;
  font-size: 0.8rem;
  color: var(--muted);
  line-height: 1.5;
}

.cancel-link {
  font-size: 0.8rem;
  color: var(--link);
  cursor: pointer;
  text-decoration: underline;
  padding: 0;
  background: none;
  border: none;
}

.cancel-link:hover {
  color: var(--link-hover);
}

/* Number map */
.map {
  display: grid;
  grid-template-columns: repeat(11, 2rem);
  gap: 3px;
}

.cell {
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.7rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  border-radius: 4px;
  cursor: pointer;
  transition: transform 150ms ease, box-shadow 150ms ease;
  user-select: none;
  position: relative;
}

.cell:hover {
  transform: scale(1.15);
  z-index: 1;
}

.cell--available {
  background: color-mix(in oklab, var(--border) 60%, var(--bg) 40%);
  color: var(--muted);
}

.cell--available:hover {
  background: color-mix(in oklab, var(--link) 20%, var(--bg) 80%);
  color: var(--link);
  box-shadow: 0 2px 8px color-mix(in oklab, var(--link) 20%, transparent);
}

.cell--taken {
  background: var(--lot-taken);
  color: var(--bg);
}

.cell--taken:hover {
  box-shadow: 0 2px 8px color-mix(in oklab, var(--lot-taken) 40%, transparent);
}

.cell--mine {
  background: var(--lot-mine);
  color: var(--bg);
}

.cell--mine:hover {
  box-shadow: 0 2px 8px color-mix(in oklab, var(--lot-mine) 40%, transparent);
}

.cell--mine-selected {
  background: var(--lot-selected);
  color: var(--bg);
  box-shadow: 0 0 0 2px var(--lot-selected-border);
}

.cell--mine-selected:hover {
  box-shadow: 0 0 0 2px var(--lot-selected-border), 0 2px 8px color-mix(in oklab, var(--lot-selected) 40%, transparent);
}

/* Legend */
.map-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
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
  background: color-mix(in oklab, var(--border) 60%, var(--bg) 40%);
}

.legend-dot--mine {
  background: var(--lot-mine);
}

.legend-dot--taken {
  background: var(--lot-taken);
}

@media (max-width: 420px) {
  .map {
    grid-template-columns: repeat(9, 2rem);
  }
}
</style>
