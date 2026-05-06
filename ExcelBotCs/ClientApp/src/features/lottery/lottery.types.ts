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
