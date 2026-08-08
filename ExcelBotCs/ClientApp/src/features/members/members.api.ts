import type { Member, MemberNote } from './members.types'
import { http } from '@/services/http'

export const MembersApi = {
  list: () => http<Member[]>('/api/members'),
  get: (id: string) => http<Member>(`/api/members/${id}`),
  create: (m: Member) => http<Member>('/api/members', { method: 'POST', body: JSON.stringify(m) }),
  update: (id: string, m: Member) => http<void>(`/api/members/${id}`, { method: 'PUT', body: JSON.stringify(m) }),
  generateLodestoneToken: (id: string) => http<{ token: string }>(`/api/members/${id}/lodestone-token`, { method: 'POST' }),
  verifyLodestone: (id: string, lodestoneInput: string) => http<{ success: boolean, message: string }>(`/api/members/${id}/verify-lodestone`, { method: 'POST', body: JSON.stringify({ LodestoneInput: lodestoneInput }) }),
  setMinecraftUsername: (id: string, minecraftUsername: string | null) => http<{ success: boolean, message: string }>(`/api/members/${id}/minecraft`, { method: 'POST', body: JSON.stringify({ MinecraftUsername: minecraftUsername }) }),

  // Note operations using dedicated endpoints
  addNote: (memberId: string, noteText: string) =>
    http<MemberNote>(`/api/members/${memberId}/notes`, {
      method: 'POST',
      body: JSON.stringify({ Note: noteText }),
    }),

  updateNote: (memberId: string, noteId: string, noteText: string) =>
    http<void>(`/api/members/${memberId}/notes/${noteId}`, {
      method: 'PUT',
      body: JSON.stringify({ Note: noteText }),
    }),

  deleteNote: (memberId: string, noteId: string) =>
    http<void>(`/api/members/${memberId}/notes/${noteId}`, {
      method: 'DELETE',
    }),
}
