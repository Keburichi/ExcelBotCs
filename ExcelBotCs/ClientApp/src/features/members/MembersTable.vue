<script setup lang="ts">
import type {Member} from './members.types'

const props = defineProps<{
  items: Member[]
  editId: string | null
  editBuffer: Member
  canEdit?: boolean
  isMember?: boolean
}>()

const emit = defineEmits<{
  'start-edit': [member: Member]
  'cancel-edit': []
  'save-edit': []
}>()
</script>

<template>
  <table>
    <thead>
    <tr>
      <th>Avatar</th>
      <th v-if="props.isMember">Name</th>
      <th>Player Name</th>
      <th v-if="props.canEdit">Subbed</th>
      <th v-if="props.isMember">Lodestone</th>
      <th v-if="props.canEdit">Actions</th>
    </tr>
    </thead>
    <tbody>
    <tr v-for="m in props.items" :key="m.Id">
      <template v-if="props.editId === m.Id">
        <td><input v-model="props.editBuffer.DiscordAvatar"/></td>
        <td><input v-model="props.editBuffer.DiscordName"/></td>
        <td><input v-model="props.editBuffer.PlayerName"/></td>
        <td class="center"><input type="checkbox" v-model="props.editBuffer.Subbed"/></td>
        <td><input v-model="props.editBuffer.LodestoneId"/></td>
        <td v-if="props.canEdit">
          <button class="btn" @click="emit('save-edit')">Save</button>
          <button class="btn secondary" @click="emit('cancel-edit')">Cancel</button>
        </td>
      </template>
      <template v-else>
        <td>
          <img v-if="m.DiscordAvatar" :src="m.DiscordAvatar" alt="avatar" class="avatar" referrerpolicy="no-referrer"/>
          <span v-else class="avatar placeholder">?</span>
        </td>
        <td v-if="props.isMember">{{ m.DiscordName }}</td>
        <td>{{ m.PlayerName }}</td>
        <td v-if="props.canEdit" class="center">{{ m.Subbed ? 'Yes' : 'No' }}</td>
        <td v-if="props.isMember">
          <template v-if="m.LodestoneId">
            <a :href="m.LodestoneId" target="_blank">{{ m.LodestoneId }}</a>
          </template>
          <template v-else>
            <span class="placeholder">No profile connected</span>
          </template>
        </td>
        <td v-if="props.canEdit">
          <button class="btn" @click="emit('start-edit', m)">Edit</button>
        </td>
      </template>
    </tr>
    </tbody>
  </table>
</template>