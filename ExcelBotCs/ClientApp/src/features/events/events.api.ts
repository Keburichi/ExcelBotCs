import type {
  ArchiveSearchParams,
  EventGroupRequest,
  ExtendEventRequest,
  FCEvent,
  OccurrenceStatus,
  PagedResult,
  Role,
} from '@/features/events/events.types'
import { http } from '@/services/http'

export const EventsApi = {
  list: (page = 1, pageSize = 50) =>
    http<PagedResult<FCEvent>>(`/api/events?page=${page}&pageSize=${pageSize}`),
  get: (id: string) => http<FCEvent>(`/api/events/${id}`),
  create: (e: FCEvent) => http<FCEvent>('/api/events', { method: 'POST', body: JSON.stringify(e) }),
  update: (id: string, e: FCEvent) => http<void>(`/api/events/${id}`, { method: 'PUT', body: JSON.stringify(e) }),
  delete: (id: string) => http<void>(`/api/events/${id}`, { method: 'DELETE' }),

  // Event-level signup
  signUp: (eventId: string, roles: Role[]) =>
    http<void>(`/api/events/${eventId}/signup`, {
      method: 'POST',
      body: JSON.stringify({ Roles: roles }),
    }),

  cancelSignup: (eventId: string) =>
    http<void>(`/api/events/${eventId}/signup`, {
      method: 'DELETE',
    }),

  // Group-based participant selection
  selectParticipants: (eventId: string, groups: EventGroupRequest[]) =>
    http<void>(`/api/events/${eventId}/participants`, {
      method: 'POST',
      body: JSON.stringify(groups),
    }),

  removeParticipant: (eventId: string, userId: string) =>
    http<void>(`/api/events/${eventId}/participants/${userId}`, {
      method: 'DELETE',
    }),

  // Occurrence management endpoints
  updateOccurrenceStatus: (eventId: string, occurrenceId: string, status: OccurrenceStatus) =>
    http<void>(`/api/events/${eventId}/occurrences/${occurrenceId}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ Status: status }),
    }),

  cancelOccurrence: (eventId: string, occurrenceId: string) =>
    http<void>(`/api/events/${eventId}/occurrences/${occurrenceId}`, {
      method: 'DELETE',
    }),

  // Archive/Restore endpoints
  listArchived: (page = 1, pageSize = 20, searchParams?: ArchiveSearchParams) => {
    const params = new URLSearchParams()
    params.append('page', String(page))
    params.append('pageSize', String(pageSize))
    if (searchParams?.searchText)
      params.append('searchText', searchParams.searchText)
    if (searchParams?.startDate)
      params.append('startDate', searchParams.startDate)
    if (searchParams?.endDate)
      params.append('endDate', searchParams.endDate)
    if (searchParams?.eventType !== undefined)
      params.append('eventType', String(searchParams.eventType))
    return http<PagedResult<FCEvent>>(`/api/events/archived?${params.toString()}`)
  },

  archive: (eventId: string) =>
    http<void>(`/api/events/${eventId}/archive`, {
      method: 'POST',
    }),

  restore: (eventId: string) =>
    http<void>(`/api/events/${eventId}/restore`, {
      method: 'POST',
    }),

  extend: (eventId: string, request: ExtendEventRequest) =>
    http<FCEvent>(`/api/events/${eventId}/extend`, {
      method: 'POST',
      body: JSON.stringify(request),
    }),
}
