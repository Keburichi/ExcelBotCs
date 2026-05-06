import type { MemberRole } from '@/features/members/members.types'
import { http } from '@/services/http'

export const AdminApi = {
  importFights: () => http<void>('/api/import/fights'),
  importDiscordMembers: () => http<void>('/api/import/members'),
  importFcMembers: () => http<void>('/api/import/lodestone'),
  importRoles: () => http<void>('/api/import/roles'),
  getRoles: () => http<MemberRole[]>('/api/memberroles'),
  updateRole: (id: string, r: MemberRole) => http<void>(`/api/memberroles/${id}`, { method: 'PUT', body: JSON.stringify(r) }),
}
