import type {
  ArchiveSearchParams,
  EventParticipant,
  ExtendEventRequest,
  FCEvent,
  OccurrenceStatus,
  Role,
} from '@/features/events/events.types'
import { http } from '@/services/http'

export const EventsApi = {
  list: (archived = false) => http<FCEvent[]>(`/api/events?archived=${archived}`),
  get: (id: string) => http<FCEvent>(`/api/events/${id}`),
  create: (e: FCEvent) => http<FCEvent>('/api/events', { method: 'POST', body: JSON.stringify(e) }),
  update: (id: string, e: FCEvent) => http<void>(`/api/events/${id}`, { method: 'PUT', body: JSON.stringify(e) }),
  delete: (id: string) => http<void>(`/api/events/${id}`, { method: 'DELETE' }),
  signUp: (event: FCEvent, role: Role) => http<void>(`/api/events/${event.Id}/signup`, { method: 'POST', body: JSON.stringify({ role }) }),
  plan: (event: FCEvent) => http<void>(`/api/events/${event.Id}/plan`, { method: 'POST', body: JSON.stringify(event) }),

  // Occurrence-specific signup endpoints
  signUpForOccurrence: (eventId: string, occurrenceId: string, roles: Role[]) =>
    http<void>(`/api/events/${eventId}/occurrences/${occurrenceId}/signup`, {
      method: 'POST',
      body: JSON.stringify({ Roles: roles }),
    }),

  cancelSignup: (eventId: string, occurrenceId: string) =>
    http<void>(`/api/events/${eventId}/occurrences/${occurrenceId}/signup`, {
      method: 'DELETE',
    }),

  // Participant selection endpoints
  selectParticipants: (eventId: string, occurrenceId: string, participants: EventParticipant[]) =>
    http<void>(`/api/events/${eventId}/occurrences/${occurrenceId}/participants`, {
      method: 'POST',
      body: JSON.stringify(participants),
    }),

  removeParticipant: (eventId: string, occurrenceId: string, userId: string) =>
    http<void>(`/api/events/${eventId}/occurrences/${occurrenceId}/participants/${userId}`, {
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
  listArchived: (searchParams?: ArchiveSearchParams) => {
    const params = new URLSearchParams()
    if (searchParams?.searchText)
      params.append('searchText', searchParams.searchText)
    if (searchParams?.startDate)
      params.append('startDate', searchParams.startDate)
    if (searchParams?.endDate)
      params.append('endDate', searchParams.endDate)
    if (searchParams?.eventType !== undefined)
      params.append('eventType', String(searchParams.eventType))
    const queryString = params.toString()
    return http<FCEvent[]>(`/api/events/archived${queryString ? `?${queryString}` : ''}`)
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
