import {http} from '@/services/http'
import type {Member} from './members.types'

export const MembersApi = {
    list: () => http<Member[]>('/api/members'),
    create: (m: Member) => http<Member>('/api/members', {method: 'POST', body: JSON.stringify(m)}),
    update: (id: string, m: Member) => http<void>(`/api/members/${id}`, {method: 'PUT', body: JSON.stringify(m)})
}