import {http} from '@/services/http'
import {FcMember} from "@/features/fcMembers/fcMembers.types";

export const FcMembersApi = {
    list: () => http<FcMember[]>('/api/fcmembers'),
    create: (m: FcMember) => http<FcMember>('/api/fcmembers', {method: 'POST', body: JSON.stringify(m)}),
    update: (id: string, m: FcMember) => http<void>(`/api/fcmembers/${id}`, {method: 'PUT', body: JSON.stringify(m)})
}