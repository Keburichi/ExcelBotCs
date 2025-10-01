import {http} from "@/services/http";
import {FCEvent, Role} from "@/features/events/events.types";


export const EventsApi = {
    list: () => http<FCEvent[]>('/api/events'),
    get: (id: string) => http<FCEvent>(`/api/events/${id}`),
    create: (e: FCEvent) => http<FCEvent>('/api/events', {method: 'POST', body: JSON.stringify(e)}),
    update: (id: string, e: FCEvent) => http<void>(`/api/events/${id}`, {method: 'PUT', body: JSON.stringify(e)}),
    delete: (id: string) => http<void>(`/api/events/${id}`, {method: 'DELETE'}),
    signUp: (event: FCEvent, role: Role) => http<void>(`/api/events/${event.Id}/signup`, {method: 'POST', body: JSON.stringify({role})}),
}