<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { useLotteryHistory } from '@/composables/useLotteryHistory'
import LotteryHistoryCard from './LotteryHistoryCard.vue'
import LotteryHistorySkeleton from './LotteryHistorySkeleton.vue'
import LotteryHistorySummary from './LotteryHistorySummary.vue'
import LotteryTabs from './LotteryTabs.vue'

const {
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
} = useLotteryHistory()

// The scroll trigger sits after the timeline. It is always rendered so the observer can attach once, and a fire
// while a batch is in flight or after the last page is simply ignored by loadMore.
const sentinel = ref<HTMLElement | null>(null)
let observer: IntersectionObserver | null = null

onMounted(() => {
  void loadFirstPage()

  // No IntersectionObserver (or an old browser) is fine: the "Load more winners" button is the fallback.
  if (typeof IntersectionObserver === 'undefined')
    return

  observer = new IntersectionObserver((entries) => {
    if (entries.some(entry => entry.isIntersecting))
      void loadMore()
  }, { rootMargin: '320px 0px' })

  if (sentinel.value)
    observer.observe(sentinel.value)
})

onBeforeUnmount(() => {
  observer?.disconnect()
  observer = null
})
</script>

<template>
  <section class="history">
    <LotteryTabs />

    <header class="history-header">
      <h1 class="page-title">
        Lottery history
      </h1>
      <p class="history-subtitle">
        Every draw we have on record, newest first. Each winner gets a card, so a shared draw shows up twice.
      </p>
    </header>

    <LotteryHistorySummary
      v-if="totals"
      :totals="totals"
      :last-draw="lastDraw"
    />

    <div v-if="error && items.length === 0" class="message message--error" role="alert">
      {{ error }}
      <BaseButton
        v-if="!loading"
        state="secondary"
        size="small"
        title="Try again"
        @clicked="loadFirstPage"
      />
    </div>

    <p v-if="showSkeleton" class="visually-hidden" role="status">
      Loading the winner history…
    </p>
    <LotteryHistorySkeleton v-if="showSkeleton" />

    <p v-else-if="isEmpty" class="history-empty">
      No draws yet. Guesses are open — the first draw writes the first card here.
    </p>

    <template v-else>
      <ol class="history-timeline" aria-label="Winner timeline">
        <LotteryHistoryCard
          v-for="item in items"
          :key="item.Id"
          :item="item"
        />
      </ol>

      <div class="history-footer" aria-live="polite">
        <p v-if="loading" class="history-footer__status" role="status">
          Loading the next winners…
        </p>
        <p v-else-if="error" class="history-footer__error" role="alert">
          {{ error }}
        </p>
        <p v-else-if="allLoaded" class="history-footer__status history-footer__status--done">
          All {{ totalWinners }} winners loaded.
        </p>

        <BaseButton
          v-if="hasMore && !loading"
          state="secondary"
          title="Load more winners"
          @clicked="loadMore"
        />
      </div>
    </template>

    <div ref="sentinel" class="history-sentinel" aria-hidden="true" />
  </section>
</template>

<style scoped>
.history {
  max-width: 1100px;
  margin: 0 auto;
}

.history-header {
  margin-bottom: 1.5rem;
}

.page-title {
  margin: 0;
  font-size: 2rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  color: transparent;
}

.history-subtitle {
  margin: 0.5rem 0 0 0;
  max-width: 60ch;
  color: var(--muted);
  font-size: 0.9rem;
  line-height: 1.6;
}

.history-timeline {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.history-empty {
  padding: 3rem 1rem;
  border: 1px dashed var(--border);
  border-radius: 12px;
  color: var(--muted);
  font-size: 0.95rem;
  text-align: center;
}

.history-footer {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  min-height: 3rem;
  margin-top: 1.5rem;
}

.history-footer__status {
  margin: 0;
  color: var(--muted);
  font-size: 0.85rem;
}

.history-footer__status--done {
  color: var(--muted);
  font-weight: 600;
}

.history-footer__error {
  margin: 0;
  color: var(--msg-error-fg);
  font-size: 0.8rem;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  margin: -1px;
  padding: 0;
  overflow: hidden;
  clip: rect(0 0 0 0);
  white-space: nowrap;
  border: 0;
}

.history-sentinel {
  height: 1px;
}

.message {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
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

@media (max-width: 600px) {
  .page-title {
    font-size: 1.6rem;
  }
}
</style>
