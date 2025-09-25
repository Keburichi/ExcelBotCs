import {http} from "@/services/http";

export const AdminApi = {
    importFights: () => http<void>('/api/import/fights'),
    importMembers: () => http<void>('/api/import/members'),
    importRoles: () => http<void>('/api/import/roles'),
}