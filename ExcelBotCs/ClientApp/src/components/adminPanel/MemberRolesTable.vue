<script lang="ts" setup>
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
        <th>Is Developer</th>
        <th>Discord Id</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="role in props.items" :key="role.Id">
        <template v-if="props.memberRoleEditId === role.Id">
          <td>{{ role.Name }}</td>
          <td><input v-model="props.memberRoleEditBuffer.IsAdmin" type="checkbox"></td>
          <td><input v-model="props.memberRoleEditBuffer.IsMember" type="checkbox"></td>
          <td><input v-model="props.memberRoleEditBuffer.IsDeveloper" type="checkbox"></td>
          <td><input v-model="props.memberRoleEditBuffer.DiscordId" type="text"></td>
          <BaseButton
            size="small"
            title="Save"
            @clicked="emit('save-role-edit')"
          />
          <BaseButton
            size="small"
            state="secondary"
            title="Cancel"
            @clicked="emit('cancel-role-edit')"
          />
        </template>
        <template v-else>
          <td>{{ role.Name }}</td>
          <td>
            <input
              :checked="role.IsAdmin"
              aria-disabled="true"
              disabled
              type="checkbox"
            >
          </td>
          <td>
            <input
              :checked="role.IsMember"
              aria-disabled="true"
              disabled
              type="checkbox"
            >
          </td>
          <td>
            <input
              :checked="role.IsDeveloper"
              aria-disabled="true"
              disabled
              type="checkbox"
            >
          </td>
          <td>{{ role.DiscordId }}</td>
          <BaseButton
            size="small"
            title="Edit"
            @clicked="emit('start-role-edit', role)"
          />
        </template>
      </tr>
    </tbody>
  </table>
</template>

<style scoped>

</style>
