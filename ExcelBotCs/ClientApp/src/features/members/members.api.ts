import type { Member } from './members.types'
import { http } from '@/services/http'

export const MembersApi = {
  list: () => http<Member[]>('/api/members'),
  get: (id: string) => http<Member>(`/api/members/${id}`),
  create: (m: Member) => http<Member>('/api/members', { method: 'POST', body: JSON.stringify(m) }),
  update: (id: string, m: Member) => http<void>(`/api/members/${id}`, { method: 'PUT', body: JSON.stringify(m) }),
  generateLodestoneToken: (id: string) => http<{ token: string }>(`/api/members/${id}/lodestone-token`, { method: 'POST' }),
  verifyLodestone: (id: string, lodestoneInput: string) => http<{ success: boolean, message: string }>(`/api/members/${id}/verify-lodestone`, { method: 'POST', body: JSON.stringify({ LodestoneInput: lodestoneInput }) }),
}
