import {AdminApi} from "@/features/adminPanel/admin.api";

export function useAdmin() {
    async function importFights() {
        try {
            await AdminApi.importFights()
            alert('Fights imported')
        } catch (e) {
            alert('Error importing fights' + e)
        }
    }

    async function importMembers() {
        try {
            await AdminApi.importMembers()
            alert('Members imported')
        } catch (e) {
            alert('Error importing members' + e)
        }
    }

    async function importRoles() {
        try {
            await AdminApi.importRoles()
            alert('Roles imported')
        } catch (e) {
            alert('Error importing roles' + e)
        }
    }

    return {importFights, importMembers, importRoles}
}