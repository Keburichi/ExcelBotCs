import {http} from "@/services/http";

export const LotteryApi = {
    view: () => http<string>('/api/lottery/view'),
    guess: (guess: number) => http<string>(`/api/lottery/guess/${guess}`, {method: 'POST', body: JSON.stringify(guess)}),
    unusedNumbers: () => http<string>('/api/lottery/unused')
}