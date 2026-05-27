import { http } from '@/services/http'

export interface CacheEntityStatus {
  EntityType: string
  Count: number
  LastRefreshed: string | null
  MaxDateModified: string | null
  IsPopulated: boolean
}

export interface CacheStatusResponse {
  Entities: CacheEntityStatus[]
}

export const CacheApi = {
  getStatus: () => http<CacheStatusResponse>('/api/cache/status'),
  getEntities: (entityType: string) => http<any[]>(`/api/cache/${entityType}`),
  clear: (entityType: string) => http<void>(`/api/cache/${entityType}/clear`, { method: 'POST' }),
  fill: (entityType: string) => http<void>(`/api/cache/${entityType}/fill`, { method: 'POST' }),
  clearAll: () => http<void>('/api/cache/clear-all', { method: 'POST' }),
  fillAll: () => http<void>('/api/cache/fill-all', { method: 'POST' }),
}
