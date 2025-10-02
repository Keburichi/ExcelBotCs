import type { Ref, UnwrapRef } from 'vue'
import { ref } from 'vue'
import { LotteryApi } from '@/features/lottery/lottery.api'

export function useLottery() {
  const loading = ref(false)
  const error = ref('')
  const view = ref<string>('')
  const response = ref<string>('')
  const input = ref<number>(0)

  async function load() {
    loading.value = true
    error.value = ''
    try {
      view.value = await LotteryApi.view()
    }
    catch (e: any) {
      error.value = e.message || 'Failed to load lottery'
    }
    finally {
      loading.value = false
    }
  }

  async function guess(guess: Ref<UnwrapRef<number>, UnwrapRef<number> | number>) {
    try {
      response.value = await LotteryApi.guess(guess.value)
      await load()
    }
    catch (e: any) {
      error.value = e.message || 'Failed to load lottery'
    }
    finally {
      loading.value = false
    }
  }

  return { loading, error, view, response, load, guess, input }
}
