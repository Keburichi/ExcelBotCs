import type { AwardUsersRequest, BonusLotteryDrawResponse, BonusLotteryEntry, BonusLotteryRequest, ChangeGuessRequest, GuessInfo } from './lottery.types'
import { MembersApi } from '@/features/members/members.api'
import { http } from '@/services/http'

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
  runLottery: () => http<{ message: string }>('/api/lottery/run', { method: 'POST' }),
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
  runBonusLottery: (prize: string) =>
    http<BonusLotteryDrawResponse>('/api/lottery/bonus-lottery', {
      method: 'POST',
      body: JSON.stringify({ Prize: prize } as BonusLotteryRequest),
    }),
  getBonusLotteryEntries: () =>
    http<BonusLotteryEntry[]>('/api/lottery/bonus-lottery/entries'),
}
