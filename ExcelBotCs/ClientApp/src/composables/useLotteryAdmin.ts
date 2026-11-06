import type {
  LotteryAdminStatus,
  LotteryAnnouncementPreview,
  LotteryFieldError,
  LotteryLedgerEntry,
  LotteryOptIns,
  LotteryPayoutPage,
  LotteryPayoutStatus,
  LotteryPrizePool,
  LotterySettings,
  LotterySettingsSaveRequest,
} from '@/features/lottery/lottery.admin.types'
import { computed, ref } from 'vue'
import { LotteryAdminApi } from '@/features/lottery/lottery.api'

/** Ledger rows per batch on the Pot & payouts tab. */
export const LEDGER_PAGE_SIZE = 20

/**
 * The admin page's shared state (#114, spec §5.9). All five tabs read the same module-level refs, so a save on one
 * tab is reflected on another without prop-drilling, and the page can refresh everything after a mutation with one
 * call. The status is always the source of truth for what the next action would do.
 */
const status = ref<LotteryAdminStatus | null>(null)
const settings = ref<LotterySettings | null>(null)
const optIns = ref<LotteryOptIns | null>(null)
const prizes = ref<LotteryPrizePool | null>(null)
const payoutPage = ref<LotteryPayoutPage | null>(null)
const payoutFilter = ref<LotteryPayoutStatus | ''>('')
const ledger = ref<LotteryLedgerEntry[]>([])
const ledgerCursor = ref<string | null>(null)

const previewVariant = ref<'win' | 'nowin'>('nowin')
const preview = ref<LotteryAnnouncementPreview | null>(null)
const previewOpen = ref(false)

const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const fieldErrors = ref<Record<string, string>>({})

const hasMoreLedger = computed(() => ledgerCursor.value !== null)
const pendingPayouts = computed(() => status.value?.PendingPayouts ?? 0)
// One source of truth for the pot summary: the status payload already carries the in-pool count.
const inPoolPrizes = computed(() => status.value?.PoolPrizes ?? 0)

/**
 * A per-submit idempotency key. `crypto.randomUUID` needs a secure context, so a deployment served over plain HTTP
 * on a LAN falls back to a timestamp-and-random key rather than throwing on click.
 */
function newIdempotencyKey(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function')
    return crypto.randomUUID()

  return `${Date.now()}-${Math.random().toString(36).slice(2)}`
}

/** Clears the page's one-line messages; tabs call it before starting a new action. */
function clearMessages() {
  error.value = ''
  notice.value = ''
  fieldErrors.value = {}
}

function applyFieldErrors(errors: LotteryFieldError[]) {
  const map: Record<string, string> = {}
  for (const fieldError of errors)
    map[fieldError.Field] = fieldError.Message
  fieldErrors.value = map
}

/**
 * Loads everything the five tabs show. One page-level refresh after a mutation keeps the pill, the counts and the
 * tables consistent, which matters more here than shaving a few requests off an admin page.
 */
async function loadAll() {
  loading.value = true
  clearMessages()

  const [statusResult, settingsResult, optInsResult, prizesResult, payoutsResult, ledgerResult] = await Promise.all([
    LotteryAdminApi.status(),
    LotteryAdminApi.settings(),
    LotteryAdminApi.optIns(),
    LotteryAdminApi.prizes(),
    LotteryAdminApi.payouts(payoutFilter.value),
    LotteryAdminApi.ledger(null, LEDGER_PAGE_SIZE),
  ])

  if (statusResult.Ok && statusResult.Data)
    status.value = statusResult.Data
  if (settingsResult.Ok && settingsResult.Data)
    settings.value = settingsResult.Data
  if (optInsResult.Ok && optInsResult.Data)
    optIns.value = optInsResult.Data
  if (prizesResult.Ok && prizesResult.Data)
    prizes.value = prizesResult.Data
  if (payoutsResult.Ok && payoutsResult.Data)
    payoutPage.value = payoutsResult.Data
  if (ledgerResult.Ok && ledgerResult.Data) {
    ledger.value = ledgerResult.Data.Items
    ledgerCursor.value = ledgerResult.Data.NextCursor
  }

  if (!statusResult.Ok)
    error.value = statusResult.Message
  else if (!settingsResult.Ok)
    error.value = settingsResult.Message

  loading.value = false
}

/** A lighter refresh after a mutation: the status and the pool/table the mutation touched. */
async function refreshAfterMutation() {
  const [statusResult, prizesResult, payoutsResult, optInsResult, ledgerResult] = await Promise.all([
    LotteryAdminApi.status(),
    LotteryAdminApi.prizes(),
    LotteryAdminApi.payouts(payoutFilter.value),
    LotteryAdminApi.optIns(),
    LotteryAdminApi.ledger(null, LEDGER_PAGE_SIZE),
  ])

  if (statusResult.Ok && statusResult.Data)
    status.value = statusResult.Data
  if (prizesResult.Ok && prizesResult.Data)
    prizes.value = prizesResult.Data
  if (payoutsResult.Ok && payoutsResult.Data)
    payoutPage.value = payoutsResult.Data
  if (optInsResult.Ok && optInsResult.Data)
    optIns.value = optInsResult.Data
  if (ledgerResult.Ok && ledgerResult.Data) {
    ledger.value = ledgerResult.Data.Items
    ledgerCursor.value = ledgerResult.Data.NextCursor
  }
}

/**
 * Runs one mutation with the shared busy flag, then reloads and reports the server's own sentence. Every admin
 * response carries a typed `Message`, so the page never invents a success line for a disposition it did not expect.
 */
async function mutate(action: () => Promise<{ Ok: boolean, Message: string }>, reload = true): Promise<boolean> {
  clearMessages()
  busy.value = true
  try {
    const result = await action()
    if (!result.Ok) {
      error.value = result.Message
      return false
    }

    if (reload)
      await refreshAfterMutation()
    notice.value = result.Message || 'Done.'
    return true
  }
  finally {
    busy.value = false
  }
}

// --- Schedule & Messages ---------------------------------------------------------------------------------------

/**
 * Saves the whole editable set (save is the only commit). Per-field rejections are returned, and their messages are
 * left on `fieldErrors` so the form can mark the inputs instead of showing one generic banner.
 */
async function saveSettings(request: LotterySettingsSaveRequest): Promise<boolean> {
  clearMessages()
  busy.value = true
  try {
    const result = await LotteryAdminApi.saveSettings(request)
    if (!result.Ok) {
      if (result.Data?.Errors?.length)
        applyFieldErrors(result.Data.Errors)
      error.value = result.Message || 'The settings were rejected.'
      return false
    }

    if (result.Data?.Settings)
      settings.value = result.Data.Settings
    if (status.value)
      await refreshAfterMutation()

    notice.value = result.Data?.Changed ? 'Settings saved.' : 'Settings already matched the form; nothing changed.'
    return true
  }
  finally {
    busy.value = false
  }
}

/** The Overview's pause toggle: the same audited settings save, with only the flag changed. */
async function setPause(paused: boolean): Promise<boolean> {
  if (!settings.value) {
    error.value = 'The settings have not loaded yet. Reload the page and try again.'
    return false
  }

  const current = settings.value
  const request: LotterySettingsSaveRequest = {
    RunDay: current.RunDay,
    RunTime: current.RunTime,
    RunTimezone: current.RunTimezone,
    ReminderDay: current.ReminderDay,
    ReminderTime: current.ReminderTime,
    ReminderTimezone: current.ReminderTimezone,
    RemindersEnabled: current.RemindersEnabled,
    PauseAutomation: paused,
    WinTemplate: current.WinTemplate,
    NoWinTemplate: current.NoWinTemplate,
    ResetTemplates: false,
  }

  return saveSettings(request)
}

// --- Pot, ledger, prizes and payouts ----------------------------------------------------------------------------

async function adjustPot(amount: number, direction: 'increase' | 'decrease', reason: string): Promise<boolean> {
  clearMessages()
  busy.value = true
  try {
    // The key is minted per submit so a double click cannot move the pot twice, while the retry of the same unedited
    // form still replays the stored entry.
    const result = await LotteryAdminApi.adjustPot({
      Amount: amount,
      Reason: reason,
      Direction: direction,
      IdempotencyKey: newIdempotencyKey(),
    })

    if (!result.Ok) {
      if (result.Data?.Errors?.length)
        applyFieldErrors(result.Data.Errors)
      error.value = result.Message || 'The adjustment was rejected.'
      return false
    }

    await refreshAfterMutation()
    const applied = result.Data?.AppliedAmount ?? 0
    notice.value = applied === 0
      ? 'The pot is already at the 1,000,000 Gil floor, so nothing moved. The attempt is in the ledger.'
      : 'The pot was adjusted. The ledger has the entry.'
    return true
  }
  finally {
    busy.value = false
  }
}

async function loadMoreLedger(): Promise<void> {
  if (ledgerCursor.value === null)
    return

  const result = await LotteryAdminApi.ledger(ledgerCursor.value, LEDGER_PAGE_SIZE)
  if (!result.Ok || !result.Data) {
    error.value = result.Message
    return
  }

  const seen = new Set(ledger.value.map(entry => entry.Id))
  ledger.value = [...ledger.value, ...result.Data.Items.filter(entry => !seen.has(entry.Id))]
  ledgerCursor.value = result.Data.NextCursor
}

async function setPayoutFilter(filter: LotteryPayoutStatus | ''): Promise<void> {
  payoutFilter.value = filter
  const result = await LotteryAdminApi.payouts(filter)
  if (!result.Ok || !result.Data) {
    error.value = result.Message
    return
  }

  payoutPage.value = result.Data
}

/** Appends the next page of the current filter; the keyset never repeats or skips a shared draw. */
async function loadMorePayouts(): Promise<void> {
  const cursor = payoutPage.value?.NextCursor
  if (!cursor)
    return

  const result = await LotteryAdminApi.payouts(payoutFilter.value, cursor)
  if (!result.Ok || !result.Data) {
    error.value = result.Message
    return
  }

  const seen = new Set((payoutPage.value?.Items ?? []).map(item => item.Id))
  payoutPage.value = {
    Items: [...(payoutPage.value?.Items ?? []), ...result.Data.Items.filter(item => !seen.has(item.Id))],
    NextCursor: result.Data.NextCursor,
    PendingCount: result.Data.PendingCount,
  }
}

async function updatePayout(roundKey: string, winnerId: string, next: LotteryPayoutStatus,
  note: string | null): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.updatePayout(roundKey, winnerId, { Status: next, Note: note })
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

async function addPrize(name: string): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.addPrize(name)
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

async function removePrize(id: string): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.removePrize(id)
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

async function assignPrize(id: string, roundKey: string, winnerId: string): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.assignPrize(id, { RoundKey: roundKey, WinnerId: winnerId })
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

// --- Reminders --------------------------------------------------------------------------------------------------

async function removeOptIn(discordId: string): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.removeOptIn(discordId)
    return { Ok: result.Ok, Message: result.Message || `Removed ${discordId} from the reminder list.` }
  })
}

async function sendTestReminder(): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.sendTestReminder()
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  }, false)
}

// --- Announcement preview ---------------------------------------------------------------------------------------

async function openPreview(variant: 'win' | 'nowin'): Promise<void> {
  clearMessages()
  previewVariant.value = variant
  preview.value = null
  previewOpen.value = true
  busy.value = true
  try {
    const result = await LotteryAdminApi.preview(variant)
    if (!result.Ok || !result.Data) {
      error.value = result.Message
      return
    }

    preview.value = result.Data
  }
  finally {
    busy.value = false
  }
}

function closePreview() {
  previewOpen.value = false
}

// --- Manual round actions ---------------------------------------------------------------------------------------

async function runNow(): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.run()
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

async function skipWeek(reason: string): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.skip(reason)
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

async function voidLastDraw(reason: string): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.voidLastDraw(reason)
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

async function retryAnnouncement(): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.retry()
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

async function dryRunNow(): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.dryRun()
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

// Moves one scheduled round (spec §5.1 "Reschedule"): the round key follows the new local date, guesses stay and the
// pot is untouched until the run. A target date that already has a round comes back as a typed conflict.
async function rescheduleRound(roundKey: string | null, scheduledAt: Date): Promise<boolean> {
  return mutate(async () => {
    const result = await LotteryAdminApi.reschedule(roundKey, scheduledAt)
    return { Ok: result.Ok, Message: result.Data?.Message ?? result.Message }
  })
}

export function useLotteryAdmin() {
  return {
    // state
    status,
    settings,
    optIns,
    prizes,
    payoutPage,
    payoutFilter,
    ledger,
    preview,
    previewVariant,
    previewOpen,
    loading,
    busy,
    error,
    notice,
    fieldErrors,
    // derived
    hasMoreLedger,
    pendingPayouts,
    inPoolPrizes,
    // actions
    clearMessages,
    loadAll,
    refreshAfterMutation,
    saveSettings,
    setPause,
    adjustPot,
    loadMoreLedger,
    setPayoutFilter,
    loadMorePayouts,
    updatePayout,
    addPrize,
    removePrize,
    assignPrize,
    removeOptIn,
    sendTestReminder,
    openPreview,
    closePreview,
    runNow,
    skipWeek,
    voidLastDraw,
    retryAnnouncement,
    dryRunNow,
    rescheduleRound,
  }
}
