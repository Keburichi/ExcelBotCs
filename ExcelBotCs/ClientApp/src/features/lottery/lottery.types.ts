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

export interface BonusLotteryEntry {
  DiscordId: number
  DiscordName: string
  Reason: string
}

export interface BonusLotteryDrawResponse {
  HasWinner: boolean
  Winner: BonusLotteryEntry | null
  AllEntries: BonusLotteryEntry[]
  Prize: string
  WinnerIndex: number
}

export interface BonusLotteryRequest {
  Prize: string
}
