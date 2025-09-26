import {http} from "@/services/http";
import {MemberRole} from "@/features/members/members.types";

export const AdminApi = {
    importFights: () => http<void>('/api/import/fights'),
    importMembers: () => http<void>('/api/import/members'),
    importRoles: () => http<void>('/api/import/roles'),
    getRoles: () => http<MemberRole[]>('/api/memberroles'),
    updateRole: (id: string, r: MemberRole) => http<void>(`/api/memberroles/${id}`, {method: 'PUT', body: JSON.stringify(r)}) 
}