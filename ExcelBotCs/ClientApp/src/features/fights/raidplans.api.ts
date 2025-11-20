import type { Raidplan } from '@/features/fights/fights.types'
import { http } from '@/services/http'

export const RaidplansApi = {
  list: (fightId: string) => http<Raidplan[]>(`/api/fights/${fightId}/raidplans`),
  get: (fightId: string, id: string) => http<Raidplan>(`/api/fights/${fightId}/raidplans/${id}`),
  create: (fightId: string, r: Raidplan) => http<Raidplan>(`/api/fights/${fightId}/raidplans`, {
    method: 'POST',
    body: JSON.stringify(r),
  }),
  update: (fightId: string, id: string, r: Raidplan) => http<void>(`/api/fights/${fightId}/raidplans/${id}`, {
    method: 'PUT',
    body: JSON.stringify(r),
  }),
  delete: (fightId: string, id: string) => http<void>(`/api/fights/${fightId}/raidplans/${id}`, {
    method: 'DELETE',
  }),
}
