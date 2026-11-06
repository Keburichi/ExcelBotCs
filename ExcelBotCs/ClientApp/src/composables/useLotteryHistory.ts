import type {
  LotteryHistoryItem,
  LotteryHistoryTotals,
  LotteryLastDraw,
} from '@/features/lottery/lottery.types'
import { computed, ref } from 'vue'
import { LotteryApi } from '@/features/lottery/lottery.api'

/** Winners per batch. The API defaults to 10 as well; the page never asks for a size the API clamps. */
export const HISTORY_PAGE_SIZE = 10

/**
 * The member history timeline's lazy loading (spec §5.8, #113): the first batch of 10, then one more batch per
 * load. Nothing here knows how the next batch is triggered — the view scrolls near the bottom with an
 * `IntersectionObserver` and also offers a keyboard-accessible "Load more winners" button, both calling `loadMore`.
 */
export function useLotteryHistory() {
  const items = ref<LotteryHistoryItem[]>([])
  const totals = ref<LotteryHistoryTotals | null>(null)
  const nextCursor = ref<string | null>(null)
  const loading = ref(false)
  const initialized = ref(false)
  const error = ref('')

  const hasMore = computed(() => nextCursor.value !== null)
  const showSkeleton = computed(() => loading.value && !initialized.value)
  const isEmpty = computed(
    () => initialized.value && !loading.value && !error.value && items.value.length === 0,
  )
  const totalWinners = computed(() => totals.value?.Winners ?? items.value.length)
  const allLoaded = computed(
    () => initialized.value && !loading.value && !hasMore.value && items.value.length > 0,
  )

  /**
   * The newest draw, taken from the leading rows of the first batch: they all share the newest `RoundKey`, so a
   * shared draw lists every winner it has loaded. The combined Gil is their share of the pot.
   */
  const lastDraw = computed<LotteryLastDraw | null>(() => {
    const newest = items.value[0]
    if (!newest)
      return null

    const winners = items.value.filter(item => item.RoundKey === newest.RoundKey)
    return {
      date: newest.Date,
      roundKey: newest.RoundKey,
      winners,
      combinedGil: winners.reduce((sum, winner) => sum + winner.GilPrize, 0),
    }
  })

  async function loadFirstPage() {
    if (loading.value)
      return

    loading.value = true
    error.value = ''
    try {
      const page = await LotteryApi.history(null, HISTORY_PAGE_SIZE)
      items.value = page.Items
      totals.value = page.Totals
      nextCursor.value = page.NextCursor ?? null
    }
    catch {
      items.value = []
      totals.value = null
      nextCursor.value = null
      error.value = 'Could not load the winner history. Please try again.'
    }
    finally {
      loading.value = false
      initialized.value = true
    }
  }

  async function loadMore() {
    // A double fire from the observer and the button must not fetch the same cursor twice.
    if (loading.value || nextCursor.value === null)
      return

    loading.value = true
    error.value = ''
    try {
      const page = await LotteryApi.history(nextCursor.value, HISTORY_PAGE_SIZE)
      const seen = new Set(items.value.map(item => item.Id))
      items.value = [...items.value, ...page.Items.filter(item => !seen.has(item.Id))]
      totals.value = page.Totals
      nextCursor.value = page.NextCursor ?? null
    }
    catch {
      // The loaded batches stay; retrying reuses the same cursor, so nothing is skipped.
      error.value = 'Could not load more winners. Please try again.'
    }
    finally {
      loading.value = false
    }
  }

  return {
    items,
    totals,
    loading,
    error,
    hasMore,
    showSkeleton,
    isEmpty,
    totalWinners,
    allLoaded,
    lastDraw,
    loadFirstPage,
    loadMore,
  }
}
