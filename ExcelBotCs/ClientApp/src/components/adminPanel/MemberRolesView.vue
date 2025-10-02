<script setup lang="ts">
import { onMounted } from 'vue'
import MemberRolesTable from '@/components/adminPanel/MemberRolesTable.vue'
import { useAdmin } from '@/composables/useAdmin'
import { useAuth } from '@/composables/useAuth'

const admin = useAdmin()
const { isAdmin } = useAuth()

onMounted(admin.getMemberRoles)
</script>

<template>
  <section class="home">
    <h2>Roles ({{ admin.memberRoles.value.length }})</h2>
    <p v-if="admin.error" class="error">
      {{ admin.error }}
    </p>

    <MemberRolesTable
      v-if="isAdmin"
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
