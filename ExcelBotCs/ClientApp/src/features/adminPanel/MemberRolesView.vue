<script setup lang="ts">
import {useAdmin} from "@/features/adminPanel/useAdmin";
import {useAuth} from "@/features/auth/useAuth";
import {onMounted} from "vue";
import MemberRolesTable from "@/features/adminPanel/MemberRolesTable.vue";

const admin = useAdmin()
const {isAdmin, ensureAuth} = useAuth()

onMounted(admin.getMemberRoles)
</script>

<template>
  <section class="home">
    <h2>Roles ({{admin.memberRoles.value.length}})</h2>
    <p v-if="admin.error" class="error">{{ admin.error }}</p>

    <MemberRolesTable v-if="isAdmin"
                      :items="admin.memberRoles.value"
                      :member-role-edit-buffer="admin.memberRoleEditBuffer"
                      :member-role-edit-id="admin.memberRoleEditId.value"
                      @start-role-edit="admin.startRoleEdit"
                      @cancel-role-edit="admin.cancelRoleEdit"
                      @save-role-edit="admin.saveMemberRole"
    />
  </section>
</template>

<style scoped>

</style>