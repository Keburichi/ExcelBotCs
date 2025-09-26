<script setup lang="ts">

import {MemberRole} from "@/features/members/members.types";

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
        <td><input type="checkbox" v-model="props.memberRoleEditBuffer.IsAdmin"/></td>
        <td><input type="checkbox" v-model="props.memberRoleEditBuffer.IsMember"/></td>
        <td><input type="text" v-model="props.memberRoleEditBuffer.DiscordId"/></td>
        <button class="btn" @click="emit('save-role-edit')">Save</button>
        <button class="btn" @click="emit('cancel-role-edit')">Cancel</button>
      </template>
      <template v-else>
        <td>{{ role.Name }}</td>
        <td><input type="checkbox" v-model="role.IsAdmin"/></td>
        <td><input type="checkbox" v-model="role.IsMember"/></td>
        <td>{{ role.DiscordId }}</td>
        <button class="btn" @click="emit('start-role-edit', role)">Edit</button>
      </template>
    </tr>
    </tbody>
  </table>
</template>

<style scoped>

</style>