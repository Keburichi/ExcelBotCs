<script setup lang="ts">
import type { LotteryAdminRound } from '@/features/lottery/lottery.admin.types'
import { computed } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { formatGil, formatInstant } from '@/features/lottery/lottery.format'
import { useLotteryAdmin } from '@/composables/useLotteryAdmin'
import LotteryAnnouncementPreview from './LotteryAnnouncementPreview.vue'

const admin = useLotteryAdmin()

const status = admin.status
const settings = admin.settings
const busy = admin.busy

const pillClass = computed(() => {
  const kind = status.value?.AutomationStatus ?? 'disabled'
  return `pill pill--${kind === 'dry-run' ? 'dry' : kind}`
})

const nextRun = computed(() => {
  const value = status.value
  if (!value)
    return '—'

  const label = `${value.NextRoundKey ?? 'next round'} · ${formatInstant(value.NextRunAtUtc, settings.value?.RunTimezone)}`
  return label
})

const thisWeek = computed(() => {
  const pending = status.value?.PendingRound
  if (pending)
    return `${pending.RoundKey} · ${pending.Status}`

  return `${status.value?.NextRoundKey ?? 'next round'} · scheduled`
})

const reminderSummary = computed(() => {
  const value = status.value
  if (!value)
    return '—'
  if (!value.RemindersEnabled)
    return 'Off'

  return `On · ${value.OptedIn} opted in, ${value.EligibleReminders} eligible`
})

const lastDraw = computed<LotteryAdminRound | null>(() => status.value?.LatestRound ?? null)

const failure = computed(() => {
  const pending = status.value?.PendingRound
  if (!pending)
    return null

  return {
    roundKey: pending.RoundKey,
    status: pending.Status,
    step: pending.FailedStep ?? pending.Status,
    error: pending.LastError ?? status.value?.LastError ?? 'No error recorded.',
    attempts: pending.Attempts,
    nextRetry: pending.NextRetryAt,
  }
})

const canRetry = computed(() => Boolean(status.value?.PendingRound))
const canVoid = computed(() => {
  const round = status.value?.LatestRound
  if (!round)
    return false

  return ['drawn', 'announced', 'completed', 'failed'].includes(round.Status)
})

function confirmRun() {
  return window.confirm(
    'Run the lottery now? This draws the winning number, writes the winners and the pot ledger, and posts the '
    + 'announcement to the lottery channel. It cannot be undone — only voided afterwards.',
  )
}

async function runNow() {
  if (!confirmRun())
    return

  await admin.runNow()
}

async function dryRun() {
  await admin.dryRunNow()
}

async function skipWeek() {
  const reason = window.prompt('Skip this week? Guesses carry over and the pot does not move.\n\nOptional reason:')
  if (reason === null)
    return

  await admin.skipWeek(reason)
}

async function voidLastDraw() {
  const reason = window.prompt(
    'Void the last draw? Its ledger entries are reversed and the round is annotated. This is audited and cannot be '
    + 'undone silently.\n\nReason (required):',
  )
  if (reason === null || reason.trim().length === 0)
    return

  await admin.voidLastDraw(reason.trim())
}

async function retryAnnouncement() {
  await admin.retryAnnouncement()
}

function togglePause() {
  const paused = !(status.value?.PauseAutomation ?? false)
  const message = paused
    ? 'Pause the automated lottery? Scheduled runs stop; Run now stays available.'
    : 'Resume the automated lottery? The scheduler will run the configured slot again.'
  if (!window.confirm(message))
    return

  void admin.setPause(paused)
}
</script>

<template>
  <div class="overview">
    <p v-if="!status" class="empty">
      The lottery status has not loaded yet. Reload the page and try again.
    </p>

    <template v-else>
      <section class="card card--wide" aria-label="This week">
        <div class="card__head">
          <h2 class="card__title">
            This week
          </h2>
          <span :class="pillClass">{{ status.AutomationLabel }}</span>
        </div>

        <dl class="facts">
          <div class="fact">
            <dt>Status</dt>
            <dd>{{ thisWeek }}</dd>
          </div>
          <div class="fact">
            <dt>Next run</dt>
            <dd>{{ nextRun }}</dd>
          </div>
          <div class="fact">
            <dt>Pot</dt>
            <dd class="fact__strong">
              {{ formatGil(status.Pot) }} Gil
              <span v-if="status.PrizeCount > 0" class="fact__muted">+ {{ status.PrizeCount }} prize{{ status.PrizeCount === 1 ? '' : 's' }}</span>
            </dd>
          </div>
          <div class="fact">
            <dt>Reminders</dt>
            <dd>{{ reminderSummary }}</dd>
          </div>
        </dl>

        <div class="actions">
          <BaseButton
            state="secondary"
            size="small"
            title="Preview winner post"
            :disabled="busy"
            @clicked="admin.openPreview('win')"
          />
          <BaseButton
            state="secondary"
            size="small"
            title="Preview no-winner post"
            :disabled="busy"
            @clicked="admin.openPreview('nowin')"
          />
          <BaseButton
            state="danger"
            size="small"
            title="Run now"
            :disabled="busy"
            @clicked="runNow"
          />
          <BaseButton
            state="tertiary"
            size="small"
            title="Dry run now"
            :disabled="busy"
            @clicked="dryRun"
          />
          <BaseButton
            state="warning"
            size="small"
            title="Skip week"
            :disabled="busy"
            @clicked="skipWeek"
          />
        </div>

        <label class="toggle">
          <input
            :checked="status.PauseAutomation"
            :disabled="busy"
            type="checkbox"
            @change="togglePause"
          >
          <span>Pause automation</span>
        </label>
        <p class="hint">
          A pause stops the scheduler; the manual <strong>Run now</strong> stays live so a missed draw can still be run.
        </p>
      </section>

      <section v-if="failure" class="card card--danger" aria-label="Failure">
        <div class="card__head">
          <h2 class="card__title">
            Needs a decision
          </h2>
          <span class="badge badge--danger">{{ failure.step }} failed</span>
        </div>
        <p class="failure-text">
          Round {{ failure.roundKey }} is {{ failure.status }} after {{ failure.attempts }} attempt{{ failure.attempts === 1 ? '' : 's' }}.
          It blocks the next scheduled round until it is retried or voided.
        </p>
        <p class="error-line">
          {{ failure.error }}
        </p>
        <p v-if="failure.nextRetry" class="hint">
          Next retry: {{ formatInstant(failure.nextRetry, settings?.RunTimezone) }}
        </p>
      </section>

      <section class="card" aria-label="Recovery and danger">
        <h2 class="card__title">
          Recovery &amp; danger
        </h2>
        <p class="hint">
          Retry resumes the unfinished round without drawing again. Void reverses the round's ledger entries and marks
          the draw voided; neither can be silently undone.
        </p>
        <div class="actions">
          <BaseButton
            v-if="canRetry"
            state="secondary"
            size="small"
            title="Retry announcement"
            :disabled="busy"
            @clicked="retryAnnouncement"
          />
          <BaseButton
            state="danger"
            size="small"
            title="Void last draw"
            :disabled="busy || !canVoid"
            @clicked="voidLastDraw"
          />
        </div>
        <p v-if="!canRetry" class="hint">
          Nothing is pending, so there is no announcement to retry.
        </p>
      </section>

      <section class="card" aria-label="Needs attention">
        <h2 class="card__title">
          Needs attention
        </h2>
        <ul class="attention">
          <li>
            <span class="attention__count">{{ status.PendingPayouts }}</span>
            <span>pending payout{{ status.PendingPayouts === 1 ? '' : 's' }}</span>
          </li>
          <li>
            <span class="attention__count">{{ status.PoolPrizes }}</span>
            <span>prize{{ status.PoolPrizes === 1 ? '' : 's' }} in the pool</span>
          </li>
          <li>
            <span class="attention__count">{{ status.OptedIn }}</span>
            <span>member{{ status.OptedIn === 1 ? '' : 's' }} opted in to reminders</span>
          </li>
        </ul>
      </section>

      <section class="card" aria-label="Last draw">
        <h2 class="card__title">
          Last draw
        </h2>
        <dl v-if="lastDraw" class="facts">
          <div class="fact">
            <dt>Round</dt>
            <dd>{{ lastDraw.RoundKey }} · {{ lastDraw.Status }}</dd>
          </div>
          <div class="fact">
            <dt>Winning number</dt>
            <dd class="fact__strong">
              {{ lastDraw.WinningNumber ?? '—' }}
            </dd>
          </div>
          <div class="fact">
            <dt>Pot</dt>
            <dd>
              <template v-if="lastDraw.PotBefore !== null && lastDraw.PotAfter !== null">
                {{ formatGil(lastDraw.PotBefore) }} → {{ formatGil(lastDraw.PotAfter) }} Gil
              </template>
              <template v-else>
                —
              </template>
            </dd>
          </div>
          <div class="fact">
            <dt>Ran</dt>
            <dd>{{ formatInstant(lastDraw.RunAt, settings?.RunTimezone) }}</dd>
          </div>
        </dl>
        <p v-else class="empty">
          No draw has run yet.
        </p>
      </section>

      <section class="card" aria-label="Last settings change">
        <h2 class="card__title">
          Last settings change
        </h2>
        <p v-if="status.LastSettingsChange" class="last-change">
          v{{ status.LastSettingsChange.Version }} on
          {{ formatInstant(status.LastSettingsChange.ChangedAtUtc, settings?.RunTimezone) }}
          <template v-if="status.LastSettingsChange.ActorDiscordId">
            by <code>{{ status.LastSettingsChange.ActorDiscordId }}</code>
          </template>
          <span v-if="status.LastSettingsChange.Fields.length"> · {{ status.LastSettingsChange.Fields.join(', ') }}</span>
        </p>
        <p v-else class="empty">
          The settings have not been changed since the defaults were deployed.
        </p>
        <p v-if="status.InvalidSettingsFields.length" class="hint hint--warning">
          The stored settings had to be repaired for: {{ status.InvalidSettingsFields.join(', ') }}. Saving the form
          on the Schedule tab clears this.
        </p>
      </section>

      <section class="card" aria-label="Deployment configuration">
        <h2 class="card__title">
          Deployment config <span class="card__note">read-only</span>
        </h2>
        <dl class="facts">
          <div class="fact">
            <dt>Lottery channel</dt>
            <dd class="mono">
              {{ status.Deployment.LotteryChannelId }}
            </dd>
          </div>
          <div class="fact">
            <dt>Member role</dt>
            <dd class="mono">
              {{ status.Deployment.LotteryMemberRoleId }}
            </dd>
          </div>
          <div class="fact">
            <dt>Emoji id</dt>
            <dd class="mono">
              {{ status.Deployment.LotteryCustomEmojiId }}
            </dd>
          </div>
          <div class="fact">
            <dt>Web base URL</dt>
            <dd class="mono">
              {{ status.Deployment.WebBaseUrl || '—' }}
            </dd>
          </div>
        </dl>
      </section>

      <LotteryAnnouncementPreview />
    </template>
  </div>
</template>

<style scoped>
.overview {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
  align-items: start;
}

.card {
  padding: 1.25rem;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--card);
}

.card--wide {
  grid-column: 1 / -1;
}

.card--danger {
  grid-column: 1 / -1;
  border-color: var(--msg-error-border);
  background: color-mix(in oklab, var(--msg-error-bg) 40%, var(--card));
}

.card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.card__title {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
}

.card__note {
  margin-left: 0.375rem;
  font-size: 0.7rem;
  font-weight: 500;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.pill {
  padding: 0.25rem 0.75rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.02em;
}

.pill--live {
  background: var(--msg-success-bg);
  color: var(--msg-success-fg);
  border: 1px solid var(--msg-success-border);
}

.pill--dry {
  background: var(--msg-info-bg);
  color: var(--msg-info-fg);
  border: 1px solid var(--msg-info-border);
}

.pill--paused {
  background: var(--msg-warning-bg, var(--muted-bg));
  color: var(--msg-warning-fg, var(--fg));
  border: 1px solid var(--msg-warning-border, var(--border));
}

.pill--disabled {
  background: var(--muted-bg);
  color: var(--muted);
  border: 1px solid var(--border);
}

.badge--danger {
  padding: 0.25rem 0.625rem;
  border-radius: 999px;
  background: var(--msg-error-bg);
  color: var(--msg-error-fg);
  border: 1px solid var(--msg-error-border);
  font-size: 0.75rem;
  font-weight: 700;
}

.facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr));
  gap: 0.75rem 1.25rem;
  margin: 0 0 1rem 0;
}

.fact dt {
  margin-bottom: 0.125rem;
  color: var(--muted);
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.fact dd {
  margin: 0;
  color: var(--fg);
  font-size: 0.9rem;
  line-height: 1.4;
}

.fact__strong {
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.fact__muted {
  color: var(--muted);
  font-weight: 500;
}

.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.8rem;
  word-break: break-all;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}

.toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.85rem;
  color: var(--fg);
  cursor: pointer;
}

.toggle input {
  width: 1rem;
  height: 1rem;
  accent-color: var(--link);
}

.toggle input:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.hint {
  margin: 0.5rem 0 0 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.5;
}

.hint--warning {
  color: var(--msg-warning-fg, var(--fg));
}

.failure-text {
  margin: 0 0 0.5rem 0;
  font-size: 0.85rem;
  line-height: 1.5;
  color: var(--fg);
}

.error-line {
  margin: 0 0 0.5rem 0;
  padding: 0.625rem 0.75rem;
  border-radius: 10px;
  background: var(--card);
  border: 1px solid var(--border);
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.78rem;
  white-space: pre-wrap;
  word-break: break-word;
}

.attention {
  display: flex;
  flex-wrap: wrap;
  gap: 1.25rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.attention li {
  display: flex;
  align-items: baseline;
  gap: 0.375rem;
  color: var(--muted);
  font-size: 0.85rem;
}

.attention__count {
  color: var(--fg);
  font-size: 1.25rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.empty {
  margin: 0;
  color: var(--muted);
  font-size: 0.85rem;
}

.last-change {
  margin: 0;
  font-size: 0.85rem;
  line-height: 1.6;
  color: var(--fg);
}

.last-change code {
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.8rem;
}

@media (max-width: 800px) {
  .overview {
    grid-template-columns: 1fr;
  }
}
</style>
