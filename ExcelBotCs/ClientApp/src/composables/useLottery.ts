import type { GuessInfo } from '@/features/lottery/lottery.types'
import { ref } from 'vue'
import { LotteryApi } from '@/features/lottery/lottery.api'

export function useLottery() {
  const loading = ref(false)
  const error = ref('')
  const view = ref<string>('')
  const response = ref<string>('')
  const allGuesses = ref<GuessInfo[]>([])
  const myGuesses = ref<number[]>([])
  const selectedNumber = ref<number | null>(null)

  async function load() {
    loading.value = true
    error.value = ''
    try {
      view.value = await LotteryApi.view()
      allGuesses.value = await LotteryApi.allGuesses()
      parseMyGuesses()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to load lottery'
      console.error('Failed to load lottery:', e)
    }
    finally {
      loading.value = false
    }
  }

  function parseMyGuesses() {
    myGuesses.value = []

    if (!view.value || typeof view.value !== 'string') {
      console.warn('View is not a string:', view.value)
      return
    }

    // Pattern 1: "Your guesses: 1, 5, 10" or "Your guess: 5"
    let match = view.value.match(/Your guesses?:\s*(\d+(?:,\s*\d+)*)/i)

    // Pattern 2: "Guesses: 1, 5, 10"
    if (!match) {
      match = view.value.match(/Guesses?:\s*(\d+(?:,\s*\d+)*)/i)
    }

    // Pattern 3: Numbers in square brackets [1, 5, 10]
    if (!match) {
      match = view.value.match(/\[(\d+(?:,\s*\d+)*)\]/)
    }

    if (match && match[1]) {
      myGuesses.value = match[1].split(',').map(n => Number.parseInt(n.trim(), 10)).filter(n => !isNaN(n) && n >= 1 && n <= 100)
      console.log('Matched pattern, parsed guesses:', myGuesses.value)
    }
    else {
      console.log('No match found in view string')
    }
  }

  async function guess(guessNumber: number) {
    try {
      response.value = await LotteryApi.guess(guessNumber)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to guess number'
      response.value = ''
    }
  }

  async function changeGuess(oldNumber: number, newNumber: number) {
    try {
      response.value = await LotteryApi.changeGuess(oldNumber, newNumber)
      selectedNumber.value = null
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to change guess'
      response.value = ''
    }
  }

  function selectNumber(num: number) {
    if (selectedNumber.value === num) {
      selectedNumber.value = null
    }
    else {
      selectedNumber.value = num
    }
  }

  type QuickPickMode = 'any' | 'available' | 'taken'

  async function quickPick(mode: QuickPickMode) {
    if (loading.value)
      return
    error.value = ''

    // Build candidate pools based on current guesses
    const allNumbers = Array.from({ length: 99 }, (_, i) => i + 1) // 1..99 per requirement
    const takenSet = new Set(
      (allGuesses.value || [])
        .map(g => g.Number)
        .filter(n => n >= 1 && n <= 99),
    )

    let candidates: number[] = []
    if (mode === 'any') {
      candidates = allNumbers
    }
    else if (mode === 'available') {
      candidates = allNumbers.filter(n => !takenSet.has(n))
    }
    else if (mode === 'taken') {
      candidates = Array.from(takenSet)
    }

    // If a number is selected for change, avoid picking the same number
    if (selectedNumber.value != null) {
      candidates = candidates.filter(n => n !== selectedNumber.value)
    }

    if (candidates.length === 0) {
      error.value = mode === 'available'
        ? 'No available numbers left between 1 and 99.'
        : (mode === 'taken'
            ? 'No taken numbers found between 1 and 99.'
            : 'No numbers to choose from.')
      return
    }

    const pick = candidates[Math.floor(Math.random() * candidates.length)]

    // If user selected an existing guess, treat this as a change
    if (selectedNumber.value != null) {
      await changeGuess(selectedNumber.value, pick)
    }
    else {
      await guess(pick)
    }
  }

  return {
    loading,
    error,
    view,
    response,
    allGuesses,
    myGuesses,
    selectedNumber,
    load,
    guess,
    changeGuess,
    selectNumber,
    quickPick,
  }
}
