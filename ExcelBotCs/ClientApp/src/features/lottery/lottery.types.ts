export interface LotteryUser {
  DiscordId: number
  DiscordName: string
}

export interface GuessInfo {
  Number: number
  Guessers: LotteryUser[]
}

export interface ChangeGuessRequest {
  OldNumber: number
  NewNumber: number
}

export interface AwardUsersRequest {
  Reason: string
  UserNames: string[] // Discord usernames
}

/// <summary>One winner card of the history timeline (spec §5.8).</summary>
export interface LotteryHistoryItem {
  Id: string
  RoundKey: string
  /** Card date, already formatted as DD.MM.YY by the API. */
  Date: string
  DisplayName: string
  /** Absent on historical rows, which were recorded with a name only. */
  DiscordId?: string | null
  /** Absent when the winner is no longer a member; the card falls back to initials. */
  AvatarUrl?: string | null
  Guess: number
  GilPrize: number
  PrizeItems: string[]
}

/** The newest draw, derived from the leading rows of the first history batch. */
export interface LotteryLastDraw {
  date: string
  roundKey: string
  winners: LotteryHistoryItem[]
  combinedGil: number
}

/** Summary numbers above the timeline: rows, distinct draws, pot and newest round. */
export interface LotteryHistoryTotals {
  Winners: number
  Draws: number
  CurrentPot: number
  LastDrawRoundKey?: string | null
}

/** One keyset page of the timeline; NextCursor is absent on the last page. */
export interface LotteryHistoryPage {
  Items: LotteryHistoryItem[]
  NextCursor?: string | null
  Totals: LotteryHistoryTotals
}

/** The member reminder opt-in, default off. */
export interface ReminderPreference {
  Enabled: boolean
}
