<script setup lang="ts">
import type { Member } from '@/features/members/members.types'
import BaseButton from '@/components/BaseButton.vue'
import ExperienceTags from './ExperienceTags.vue'

const props = defineProps<{
  items: Member[]
  editId: string | null
  canEdit?: boolean
  isMember?: boolean
}>()

const emit = defineEmits<{
  startEdit: [member: Member]
  cancelEdit: []
  saveEdit: []
}>()

const editBufferModel = defineModel<Member>({ required: true })
</script>

<template>
  <table>
    <thead>
      <tr>
        <th>Avatar</th>
        <th v-if="props.isMember">
          Name
        </th>
        <th>Player Name</th>
        <th v-if="props.isMember">
          Experience
        </th>
        <th v-if="props.canEdit">
          Subbed
        </th>
        <th v-if="props.isMember">
          Lodestone
        </th>
        <th v-if="props.canEdit">
          Actions
        </th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="m in props.items" :key="m.Id">
        <template v-if="props.editId === m.Id">
          <td><input v-model="editBufferModel.DiscordAvatar"></td>
          <td v-if="props.isMember">
            <input v-model="editBufferModel.DiscordName">
          </td>
          <td><input v-model="editBufferModel.PlayerName"></td>
          <td v-if="props.isMember">
            <ExperienceTags :experience="editBufferModel.Experience || []" />
          </td>
          <td v-if="props.canEdit" class="center">
            <input v-model="editBufferModel.Subbed" type="checkbox">
          </td>
          <td v-if="props.isMember">
            <input v-model="editBufferModel.LodestoneId">
          </td>
          <td v-if="props.canEdit">
            <div class="action-buttons">
              <BaseButton
                size="small"
                state="primary"
                title="Save"
                @clicked="emit('saveEdit')"
              />
              <BaseButton
                size="small"
                state="secondary"
                title="Cancel"
                variant="outlined"
                @clicked="emit('cancelEdit')"
              />
            </div>
          </td>
        </template>
        <template v-else>
          <td>
            <img v-if="m.DiscordAvatar" :src="m.DiscordAvatar" alt="avatar" class="avatar" referrerpolicy="no-referrer">
            <div v-else class="avatar avatar--placeholder" :title="`No avatar for ${m.PlayerName}`" />
          </td>
          <td v-if="props.isMember">
            {{ m.DiscordName }}
          </td>
          <td>{{ m.PlayerName }}</td>
          <td v-if="props.isMember">
            <ExperienceTags :experience="m.Experience || []" />
          </td>
          <td v-if="props.canEdit" class="center">
            {{ m.Subbed ? 'Yes' : 'No' }}
          </td>
          <td v-if="props.isMember">
            <template v-if="m.LodestoneId">
              <a :href="m.LodestoneId" target="_blank">{{ m.LodestoneId }}</a>
            </template>
            <template v-else>
              <span class="placeholder">No profile connected</span>
            </template>
          </td>
          <td v-if="props.canEdit">
            <BaseButton
              size="small"
              state="primary"
              title="Edit"
              variant="outlined"
              @clicked="emit('startEdit', m)"
            />
          </td>
        </template>
      </tr>
    </tbody>
  </table>
</template>

<style scoped>
.action-buttons {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}
</style>
