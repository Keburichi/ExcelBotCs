import type {
  AssignPrizeRequest,
  LotteryAdminStatus,
  LotteryAnnouncementPreview,
  LotteryLedgerPage,
  LotteryOptIns,
  LotteryPayoutPage,
  LotteryPayoutStatus,
  LotteryPrizePool,
  LotterySettings,
  LotterySettingsSaveRequest,
  LotterySettingsSaveResponse,
  ManualActionResponse,
  PotAdjustmentRequest,
  PotAdjustmentResponse,
  PayoutUpdateRequest,
  PayoutUpdateResponse,
  PrizeResponse,
  ReminderTestResponse,
} from './lottery.admin.types'
import type {
  AwardUsersRequest,
  ChangeGuessRequest,
  GuessInfo,
  LotteryHistoryPage,
  ReminderPreference,
} from './lottery.types'
import { MembersApi } from '@/features/members/members.api'
import { http, httpOutcome } from '@/services/http'

/**
 * The result of an admin call: the HTTP status and the parsed body, plus a message the page can show. Admin
 * endpoints answer expected failures with typed bodies (a field error, a disposition, a code), so the page needs the
 * body on a non-2xx response and `http` — which throws on one — would discard it.
 */
export interface AdminApiResult<T> {
  Ok: boolean
  Status: number
  Data: T | null
  Message: string
}

export const LotteryApi = {
  view: async () => {
    const response = await http<{ view: string, usedGuesses: number, totalGuesses: number }>('/api/lottery/view')
    return response
  },
  guess: async (guess: number) => {
    const response = await http<{ guessResponse: string }>(`/api/lottery/guess/${guess}`, {
      method: 'POST',
      body: JSON.stringify(guess),
    })
    return response.guessResponse
  },
  unusedNumbers: async () => {
    const response = await http<{ result: string }>('/api/lottery/unused')
    return response.result
  },
  changeGuess: async (oldNumber: number, newNumber: number) => {
    const response = await http<{ changeResponse: string }>('/api/lottery/change', {
      method: 'POST',
      body: JSON.stringify({ OldNumber: oldNumber, NewNumber: newNumber } as ChangeGuessRequest),
    })
    return response.changeResponse
  },
  whoGuessed: async (number: number) => {
    const response = await http<{ whoGuessed: string }>(`/api/lottery/who-guessed/${number}`)
    return response.whoGuessed
  },
  allGuesses: () => http<GuessInfo[]>('/api/lottery/all-guesses'),
  // The member history timeline: newest first, keyset paginated; omit the cursor for the first page.
  history: (cursor?: string | null, limit = 10) => {
    const params = new URLSearchParams({ limit: String(limit) })
    if (cursor)
      params.set('cursor', cursor)
    return http<LotteryHistoryPage>(`/api/lottery/history?${params.toString()}`)
  },
  // The member reminder opt-in. The PUT returns the state the server stored, so the toggle never has to guess.
  getReminders: () => http<ReminderPreference>('/api/lottery/reminders'),
  setReminders: (enabled: boolean) =>
    http<ReminderPreference>('/api/lottery/reminders', {
      method: 'PUT',
      body: JSON.stringify({ enabled }),
    }),
  // Return FC member names as a simple string[] for the UI autocomplete.
  // We map Member objects to a displayable name, preferring PlayerName, then DiscordName.
  fcMembers: async () => {
    const members = await MembersApi.list()
    const names = members
      .map(m => (m.PlayerName || m.DiscordName || '').trim())
      .filter(n => n.length > 0)
    // Deduplicate and sort for better UX
    return Array.from(new Set(names)).sort((a, b) => a.localeCompare(b))
  },
  // fcMembers: () => http<string[]>('/api/lottery/fc-members'),
  awardUsers: (reason: string, userNames: string[]) =>
    http<{ message: string }>('/api/lottery/award', {
      method: 'POST',
      body: JSON.stringify({ Reason: reason, UserNames: userNames } as AwardUsersRequest),
    }),
}

/**
 * The lottery admin API (#114, spec §5.9 and §8). Every route behind it is admin-only; the router guard keeps
 * members out and the API answers 403 if they try anyway. Expected failures come back typed, so `Message` is the
 * server's own user-facing sentence rather than a status-based guess.
 */
export const LotteryAdminApi = {
  status: () => adminRequest<LotteryAdminStatus>('/api/lottery/admin/status'),
  settings: () => adminRequest<LotterySettings>('/api/lottery/admin/settings'),
  saveSettings: (request: LotterySettingsSaveRequest) =>
    adminRequest<LotterySettingsSaveResponse>('/api/lottery/admin/settings', {
      method: 'PUT',
      body: JSON.stringify(request),
    }),
  adjustPot: (request: PotAdjustmentRequest) =>
    adminRequest<PotAdjustmentResponse>('/api/lottery/admin/pot', {
      method: 'POST',
      body: JSON.stringify(request),
    }),
  ledger: (cursor?: string | null, limit = 20) => {
    const params = new URLSearchParams({ limit: String(limit) })
    if (cursor)
      params.set('cursor', cursor)
    return adminRequest<LotteryLedgerPage>(`/api/lottery/admin/ledger?${params.toString()}`)
  },
  payouts: (status?: LotteryPayoutStatus | '', cursor?: string | null, limit = 25) => {
    const params = new URLSearchParams({ limit: String(limit) })
    if (status)
      params.set('status', status)
    if (cursor)
      params.set('cursor', cursor)
    return adminRequest<LotteryPayoutPage>(`/api/lottery/admin/payouts?${params.toString()}`)
  },
  updatePayout: (roundKey: string, winnerId: string, request: PayoutUpdateRequest) =>
    adminRequest<PayoutUpdateResponse>(
      `/api/lottery/admin/winners/${encodeURIComponent(roundKey)}/${encodeURIComponent(winnerId)}/payout`,
      { method: 'PATCH', body: JSON.stringify(request) },
    ),
  prizes: () => adminRequest<LotteryPrizePool>('/api/lottery/admin/prizes'),
  addPrize: (name: string) =>
    adminRequest<PrizeResponse>('/api/lottery/admin/prizes', {
      method: 'POST',
      body: JSON.stringify({ Name: name }),
    }),
  removePrize: (id: string) =>
    adminRequest<PrizeResponse>(`/api/lottery/admin/prizes/${encodeURIComponent(id)}`, { method: 'DELETE' }),
  assignPrize: (id: string, request: AssignPrizeRequest) =>
    adminRequest<PrizeResponse>(`/api/lottery/admin/prizes/${encodeURIComponent(id)}/assign`, {
      method: 'POST',
      body: JSON.stringify(request),
    }),
  optIns: () => adminRequest<LotteryOptIns>('/api/lottery/admin/optins'),
  removeOptIn: (discordId: string) =>
    adminRequest<null>(`/api/lottery/admin/optins/${encodeURIComponent(discordId)}`, { method: 'DELETE' }),
  sendTestReminder: () =>
    adminRequest<ReminderTestResponse>('/api/lottery/admin/reminders/test', { method: 'POST' }),
  preview: (result: 'win' | 'nowin') =>
    adminRequest<LotteryAnnouncementPreview>(`/api/lottery/admin/preview?result=${result}`),
  run: () => adminRequest<ManualActionResponse>('/api/lottery/admin/run', { method: 'POST' }),
  skip: (reason: string) =>
    adminRequest<ManualActionResponse>('/api/lottery/admin/skip', {
      method: 'POST',
      body: JSON.stringify({ Reason: reason, RoundKey: null }),
    }),
  voidLastDraw: (reason: string, roundKey?: string | null) =>
    adminRequest<ManualActionResponse>('/api/lottery/admin/void', {
      method: 'POST',
      body: JSON.stringify({ Reason: reason, RoundKey: roundKey ?? null }),
    }),
  retry: () => adminRequest<ManualActionResponse>('/api/lottery/admin/retry', { method: 'POST' }),
  dryRun: () => adminRequest<ManualActionResponse>('/api/lottery/admin/dry-run', { method: 'POST' }),
  reschedule: (roundKey: string | null, scheduledAtUtc: string | Date) =>
    adminRequest<ManualActionResponse>('/api/lottery/admin/reschedule', {
      method: 'POST',
      body: JSON.stringify({
        RoundKey: roundKey,
        ScheduledAtUtc: scheduledAtUtc instanceof Date ? scheduledAtUtc.toISOString() : scheduledAtUtc,
      }),
    }),
}

async function adminRequest<T>(input: RequestInfo, init?: RequestInit): Promise<AdminApiResult<T>> {
  // `httpOutcome` is the shared, non-throwing transport: same credentials, JSON headers and parsing as `http`, but the
  // typed body of a 4xx survives so the page can show the server's own sentence.
  const outcome = await httpOutcome<T>(input, init)

  return {
    Ok: outcome.Ok,
    Status: outcome.Status,
    Data: outcome.Data,
    Message: outcome.Ok
      ? ''
      : outcome.Unreachable
        ? 'The request could not be sent. Check your connection and try again.'
        : messageOf(outcome.Data, outcome.Status),
  }
}

/** Pulls the server's own sentence out of a typed failure body, falling back to the status text. */
function messageOf(body: unknown, status: number): string {
  if (body && typeof body === 'object') {
    const record = body as Record<string, unknown>
    const errors = record.Errors
    if (Array.isArray(errors) && errors.length > 0) {
      const first = errors[0] as Record<string, unknown>
      if (typeof first.Message === 'string')
        return first.Message
    }
    if (typeof record.Message === 'string' && record.Message.length > 0)
      return record.Message
  }

  if (status === 403)
    return 'This page is for lottery admins only.'
  if (status === 401)
    return 'Your session expired. Reload the page and sign in again.'

  return 'The server rejected the request. Please try again.'
}
