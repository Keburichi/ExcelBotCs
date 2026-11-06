<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useLotteryAdmin } from '@/composables/useLotteryAdmin'
import LotteryTabs from '../LotteryTabs.vue'
import LotteryAdminGuesses from './LotteryAdminGuesses.vue'
import LotteryAdminMessages from './LotteryAdminMessages.vue'
import LotteryAdminOverview from './LotteryAdminOverview.vue'
import LotteryAdminPotPayouts from './LotteryAdminPotPayouts.vue'
import LotteryAdminSchedule from './LotteryAdminSchedule.vue'

// The five tabs of spec §5.9, in the order the resolution fixed. The bar is a real tablist so arrow keys move
// between tabs, and each panel is labelled by its tab.
const tabs = [
  { Id: 'overview', Label: 'Overview' },
  { Id: 'schedule', Label: 'Schedule & reminders' },
  { Id: 'pot', Label: 'Pot & payouts' },
  { Id: 'messages', Label: 'Messages' },
  { Id: 'guesses', Label: 'Guesses' },
] as const

type TabId = typeof tabs[number]['Id']

const active = ref<TabId>('overview')
const tabButtons = ref<HTMLButtonElement[]>([])

const { loading, error, notice, loadAll } = useLotteryAdmin()

onMounted(loadAll)

function select(id: TabId) {
  active.value = id
}

function setTabButton(element: unknown, index: number) {
  if (element instanceof HTMLButtonElement)
    tabButtons.value[index] = element
}

function onTabKey(event: KeyboardEvent, index: number) {
  const last = tabs.length - 1
  let next: number

  switch (event.key) {
    case 'ArrowRight':
      next = index === last ? 0 : index + 1
      break
    case 'ArrowLeft':
      next = index === 0 ? last : index - 1
      break
    case 'Home':
      next = 0
      break
    case 'End':
      next = last
      break
    default:
      return
  }

  event.preventDefault()
  active.value = tabs[next].Id
  tabButtons.value[next]?.focus()
}
</script>

<template>
  <section class="admin">
    <LotteryTabs />

    <header class="admin-header">
      <h1 class="page-title">
        Lottery admin
      </h1>
      <p class="admin-subtitle">
        Everything scheduled, drawn and paid out for the weekly lottery. Actions here are live and audited;
        the member pages stay at Play and History.
      </p>
    </header>

    <p v-if="loading" class="admin-status" role="status">
      Loading the lottery state…
    </p>

    <p v-if="error" class="message message--error" role="alert">
      {{ error }}
    </p>
    <p v-if="notice" class="message message--success" role="status">
      {{ notice }}
    </p>

    <nav class="admin-tabs" role="tablist" aria-label="Lottery admin sections">
      <button
        v-for="(tab, index) in tabs"
        :id="`lottery-admin-tab-${tab.Id}`"
        :key="tab.Id"
        :ref="element => setTabButton(element, index)"
        :aria-controls="`lottery-admin-panel-${tab.Id}`"
        :aria-selected="active === tab.Id"
        :class="['admin-tab', { 'admin-tab--active': active === tab.Id }]"
        :tabindex="active === tab.Id ? 0 : -1"
        role="tab"
        type="button"
        @click="select(tab.Id)"
        @keydown="onTabKey($event, index)"
      >
        {{ tab.Label }}
      </button>
    </nav>

    <div
      v-if="active === 'overview'"
      id="lottery-admin-panel-overview"
      aria-labelledby="lottery-admin-tab-overview"
      role="tabpanel"
      tabindex="0"
    >
      <LotteryAdminOverview />
    </div>

    <div
      v-else-if="active === 'schedule'"
      id="lottery-admin-panel-schedule"
      aria-labelledby="lottery-admin-tab-schedule"
      role="tabpanel"
      tabindex="0"
    >
      <LotteryAdminSchedule />
    </div>

    <div
      v-else-if="active === 'pot'"
      id="lottery-admin-panel-pot"
      aria-labelledby="lottery-admin-tab-pot"
      role="tabpanel"
      tabindex="0"
    >
      <LotteryAdminPotPayouts />
    </div>

    <div
      v-else-if="active === 'messages'"
      id="lottery-admin-panel-messages"
      aria-labelledby="lottery-admin-tab-messages"
      role="tabpanel"
      tabindex="0"
    >
      <LotteryAdminMessages />
    </div>

    <div
      v-else
      id="lottery-admin-panel-guesses"
      aria-labelledby="lottery-admin-tab-guesses"
      role="tabpanel"
      tabindex="0"
    >
      <LotteryAdminGuesses />
    </div>
  </section>
</template>

<style scoped>
.admin {
  max-width: 1100px;
  margin: 0 auto;
}

.admin-header {
  margin-bottom: 1.25rem;
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

.admin-subtitle {
  margin: 0.5rem 0 0 0;
  max-width: 70ch;
  color: var(--muted);
  font-size: 0.9rem;
  line-height: 1.6;
}

.admin-status {
  margin: 0 0 1rem 0;
  color: var(--muted);
  font-size: 0.85rem;
}

.admin-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  margin-bottom: 1.5rem;
  padding: 0.375rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--muted-bg);
}

.admin-tab {
  padding: 0.5rem 0.875rem;
  border: none;
  border-radius: 9px;
  background: transparent;
  color: var(--muted);
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 200ms ease, color 200ms ease;
}

.admin-tab:hover {
  color: var(--fg);
}

.admin-tab:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.admin-tab--active {
  color: var(--fg);
  background: color-mix(in oklab, var(--link) 14%, var(--card));
  box-shadow: inset 0 0 0 1px color-mix(in oklab, var(--link) 24%, var(--border));
}

.message {
  padding: 0.75rem 1rem;
  border-radius: 12px;
  margin-bottom: 1rem;
  font-size: 0.875rem;
  line-height: 1.5;
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

@media (max-width: 600px) {
  .page-title {
    font-size: 1.6rem;
  }

  .admin-tabs {
    flex-direction: column;
  }

  .admin-tab {
    text-align: left;
    width: 100%;
  }
}
</style>
