<script setup lang="ts">
import type { Member, MemberNote } from '@/features/members/members.types'
import { reactive, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import MemberNoteAddDialog from '@/components/members/MemberNoteAddDialog.vue'
import MemberNoteEditDialog from '@/components/members/MemberNoteEditDialog.vue'
import { useAuth } from '@/composables/useAuth'

const props = defineProps<{
  modelValue: boolean
  member: Member
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()

const form = reactive<Member>({
  Id: '',
  DiscordName: '',
  DiscordAvatar: '',
  LodestoneId: '',
  LodestoneVerificationToken: '',
  PlayerName: '',
  Subbed: false,
  DiscordId: '',
  IsAdmin: false,
  IsMember: false,
  Experience: [],
  Notes: [],
  Roles: [],
})

const addNoteOpen = ref(false)
const addNoteBuffer = ref<MemberNote>()
const editNoteOpen = ref(false)
const editNoteBuffer = ref<MemberNote>()

const { user } = useAuth()

function addNote() {

}

function editNote(note: MemberNote) {
  editNoteBuffer.value = note
}
</script>

<template>
  <MemberNoteAddDialog v-model:is-open="editNoteOpen" v-model:member-note="addNoteBuffer" />
  <MemberNoteEditDialog v-model:is-open="editNoteOpen" v-model:member-note="editNoteBuffer" />

  <BaseModal
    :model-value="props.modelValue" :title="`Edit - ${member.DiscordName}`" size="large"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <template #body>
      <form class="form">
        <div class="form-row">
          <label>Character Id</label>
          <input v-model="form.LodestoneId" type="text" placeholder="Character Lodestone Id">
        </div>

        <div class="form-row">
          <label>Lodestone Verification Toke</label>
          <input v-model="form.LodestoneVerificationToken" type="text" placeholder="Lodestone verification token" disabled>
        </div>

        <div class="form-row">
          <input :id="member.DiscordId" v-model="form.Subbed" :name="props.member.DiscordId" type="checkbox" placeholder="Is player subbed?">
          <label :for="member.DiscordId">Subbed?</label>
        </div>

        <div class="form-row">
          <h1>Notes</h1>
          <table>
            <thead>
              <tr>
                <th>Author</th>
                <th>Note</th>
                <th />
              </tr>
            </thead>
            <tbody>
              <tr v-for="m in form.Notes" :key="m.Id">
                <td>{{ m.Author?.DiscordName }}</td>
                <td>{{ m.Note }}</td>
                <td><BaseButton title="Edit" size="small" @clicked="editNote(m)" /></td>
              </tr>
            </tbody>
          </table>
        </div>
      </form>

      <BaseButton title="Add Note" size="small" @clicked="addNoteOpen = true" />
    </template>
  </BaseModal>
</template>

<style scoped>
.form-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 12px 0;
}
</style>
