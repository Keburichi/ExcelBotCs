import type { Fight } from '@/features/fights/fights.types'
import { http } from '@/services/http'

export const FightsApi = {
  list: () => http<Fight[]>('/api/fights'),
  create: (f: Fight) => http<Fight>('/api/fights', { method: 'POST', body: JSON.stringify(f) }),
  update: (id: string, f: Fight) => http<void>(`/api/fights/${id}`, { method: 'PUT', body: JSON.stringify(f) }),
  delete: (id: string) => http<void>(`/api/fights/${id}`, { method: 'DELETE' }),
}
