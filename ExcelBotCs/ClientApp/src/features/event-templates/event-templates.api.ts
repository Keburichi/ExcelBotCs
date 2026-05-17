import type { EventTemplate } from '@/features/event-templates/event-templates.types'
import { http } from '@/services/http'

export const EventTemplatesApi = {
  list: () => http<EventTemplate[]>('/api/event-templates'),
  get: (id: string) => http<EventTemplate>(`/api/event-templates/${id}`),
  create: (t: Omit<EventTemplate, 'Id'>) =>
    http<EventTemplate>('/api/event-templates', { method: 'POST', body: JSON.stringify(t) }),
  update: (id: string, t: Omit<EventTemplate, 'Id'>) =>
    http<EventTemplate>(`/api/event-templates/${id}`, { method: 'PUT', body: JSON.stringify(t) }),
  delete: (id: string) =>
    http<void>(`/api/event-templates/${id}`, { method: 'DELETE' }),
}
