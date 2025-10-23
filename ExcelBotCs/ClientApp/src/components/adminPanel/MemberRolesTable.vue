<script setup lang="ts">
import type { MemberRole } from '@/features/members/members.types'
import BaseButton from '@/components/BaseButton.vue'

const props = defineProps<{
  items: MemberRole[]
  memberRoleEditId: string | null
  memberRoleEditBuffer: MemberRole
}>()

const emit = defineEmits<{
  'start-role-edit': [role: MemberRole]
  'cancel-role-edit': []
  'save-role-edit': []
}>()
</script>

<template>
  <table>
    <thead>
      <tr>
        <th>Name</th>
        <th>Is Admin</th>
        <th>Is Member</th>
        <th>Discord Id</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="role in props.items" :key="role.Id">
        <template v-if="props.memberRoleEditId === role.Id">
          <td>{{ role.Name }}</td>
          <td><input v-model="props.memberRoleEditBuffer.IsAdmin" type="checkbox"></td>
          <td><input v-model="props.memberRoleEditBuffer.IsMember" type="checkbox"></td>
          <td><input v-model="props.memberRoleEditBuffer.DiscordId" type="text"></td>
          <BaseButton
            title="Save"
            size="small"
            @clicked="emit('save-role-edit', role)"
          />
          <BaseButton
            title="Cancel"
            size="small"
            state="secondary"
            @clicked="emit('cancel-role-edit', role)"
          />
        </template>
        <template v-else>
          <td>{{ role.Name }}</td>
          <td><input v-model="role.IsAdmin" type="checkbox"></td>
          <td><input v-model="role.IsMember" type="checkbox"></td>
          <td>{{ role.DiscordId }}</td>
          <BaseButton
            title="Edit"
            size="small"
            @clicked="emit('start-role-edit', role)"
          />
        </template>
      </tr>
    </tbody>
  </table>
</template>

<style scoped>

</style>
