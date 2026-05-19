import type { Resource } from '@/features/fights/fights.types'
import { http } from '@/services/http'

export const ResourcesApi = {
  list: (fightId: string) => http<Resource[]>(`/api/fights/${fightId}/resources`),
  get: (fightId: string, id: string) => http<Resource>(`/api/fights/${fightId}/resources/${id}`),
  create: (fightId: string, r: Omit<Resource, 'Id' | 'FightId' | 'AuthorId'>) => http<Resource>(`/api/fights/${fightId}/resources`, {
    method: 'POST',
    body: JSON.stringify(r),
  }),
  update: (fightId: string, id: string, r: Partial<Resource>) => http<void>(`/api/fights/${fightId}/resources/${id}`, {
    method: 'PUT',
    body: JSON.stringify(r),
  }),
  delete: (fightId: string, id: string) => http<void>(`/api/fights/${fightId}/resources/${id}`, {
    method: 'DELETE',
  }),
}
