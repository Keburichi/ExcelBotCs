import {AdminApi} from "@/features/adminPanel/admin.api";
import {reactive, ref} from "vue";
import {MemberRole} from "@/features/members/members.types";

export function useAdmin() {
    const loading = ref(false)
    const error = ref('')

    // member constants
    const memberRoles = ref<MemberRole[]>([])
    const memberRoleEditId = ref<string | null>(null)
    const memberRoleEditBuffer = reactive<MemberRole>({
        Name: '', IsAdmin: false, IsMember: false, DiscordId: 0
    })

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

    function startRoleEdit(memberRole: MemberRole) {
        memberRoleEditId.value = memberRole.Id ?? null;
        Object.assign(memberRoleEditBuffer, memberRole)
    }
    
    function cancelRoleEdit() {
        memberRoleEditId.value = null
    }
    
    async function getMemberRoles() {
        loading.value = true
        error.value = ''
        try {
            memberRoles.value = await AdminApi.getRoles()
        } catch (e: any) {
            error.value = e.message || 'Failed to get roles'
        } finally {
            loading.value = false
        }
    }

    async function saveMemberRole() {
        if (!memberRoleEditId.value)
            return

        try {
            await AdminApi.updateRole(memberRoleEditId.value, memberRoleEditBuffer)
            const index = memberRoles.value.findIndex(x => x.Id === memberRoleEditId.value)
            if (index >= 0)
                memberRoles.value[index] = {...memberRoleEditBuffer, Id: memberRoleEditId.value}

            memberRoleEditId.value = null
        } catch (e: any) {
            error.value = e.message || 'Failed to update role'
        }
    }

    return {
        importFights,
        importMembers,
        importRoles,
        getMemberRoles,
        memberRoles,
        error,
        memberRoleEditId,
        memberRoleEditBuffer,
        saveMemberRole,
        startRoleEdit,
        cancelRoleEdit
    }
}