<script setup lang="ts">
import type { Member, MemberNote } from '@/features/members/members.types'
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import ExperienceTags from '@/components/members/ExperienceTags.vue'
import MemberNoteDialog from '@/components/members/MemberNoteDialog.vue'
import { useAuth } from '@/composables/useAuth'
import { MembersApi } from '@/features/members/members.api'

const router = useRouter()
const route = useRoute()
const { user, isAdmin, loadMe } = useAuth()

const loading = ref(false)
const error = ref('')
const noteLoading = ref(false)

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
const editNoteOpen = ref(false)
const editNoteBuffer = ref<MemberNote>()

function openAddNote() {
  editNoteBuffer.value = undefined
  addNoteOpen.value = true
}

function openEditNote(note: MemberNote) {
  editNoteBuffer.value = note
  editNoteOpen.value = true
}

async function handleSaveNote(note: MemberNote) {
  noteLoading.value = true
  try {
    if (note.Id) {
      // Edit existing note
      await MembersApi.updateNote(form.Id!, note.Id, note.Note)
    }
    else {
      // Add new note
      await MembersApi.addNote(form.Id!, note.Note)
    }
    // Reload member data to get updated notes
    const memberData = await MembersApi.get(route.params.id as string)
    if (memberData) {
      Object.assign(form, memberData)
    }
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to save note'
  }
  finally {
    noteLoading.value = false
  }
}

async function handleDeleteNote(note: MemberNote) {
  noteLoading.value = true
  try {
    if (note.Id) {
      await MembersApi.deleteNote(form.Id!, note.Id)
      // Reload member data to get updated notes
      const memberData = await MembersApi.get(route.params.id as string)
      if (memberData) {
        Object.assign(form, memberData)
      }
    }
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to delete note'
  }
  finally {
    noteLoading.value = false
  }
}

function formatDate(date?: string): string {
  if (!date)
    return 'N/A'
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function goBack() {
  router.push({ name: 'admin-members' })
}

onMounted(async () => {
  loading.value = true
  try {
    const memberData = await MembersApi.get(route.params.id as string)
    if (memberData) {
      Object.assign(form, memberData)
    }
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to load event'
  }
  finally {
    loading.value = false
  }
})
</script>

<template>
  <section>
    <MemberNoteDialog
      v-model="addNoteOpen"
      :is-edit="false"
      @save="handleSaveNote"
    />

    <MemberNoteDialog
      v-model="editNoteOpen"
      :note="editNoteBuffer"
      :is-edit="true"
      @save="handleSaveNote"
      @delete="handleDeleteNote"
    />

    <h1 class="text-3xl font-bold">
      Edit {{ form.DiscordName }}
    </h1>
    <p v-if="error" class="error">
      {{ error }}
    </p>
    <form class="form">
      <div class="form-row">
        <label>Character Id</label>
        <input v-model="form.LodestoneId" type="text" placeholder="Character Lodestone Id">
      </div>

      <div class="form-row">
        <label>Lodestone Verification Token</label>
        <input v-model="form.LodestoneVerificationToken" type="text" placeholder="Lodestone verification token" disabled>
      </div>

      <div class="form-row-checkbox">
        <input :id="form.DiscordId" v-model="form.Subbed" :name="form.DiscordId" type="checkbox" placeholder="Is player subbed?">
        <label :for="form.DiscordId">Subbed?</label>
      </div>

      <div class="form-row">
        <label>Experience:</label>
        <div v-if="form.Experience?.length" class="experience-container">
          <ExperienceTags :experience="form.Experience" />
        </div>
        <p v-else class="no-experience">
          No cleared content yet
        </p>
      </div>

      <div class="form-row">
        <h2 class="notes-header">
          Notes
        </h2>
        <div v-if="noteLoading" class="loading-message">
          Loading notes...
        </div>
        <div v-else-if="form.Notes && form.Notes.length > 0" class="notes-table-container">
          <table class="notes-table">
            <thead>
              <tr>
                <th>Author</th>
                <th>Note</th>
                <th>Created</th>
                <th>Last Edited</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="note in form.Notes" :key="note.Id">
                <td>{{ note.Author || 'Unknown' }}</td>
                <td class="note-content">
                  {{ note.Note }}
                </td>
                <td>{{ formatDate(note.CreateDate) }}</td>
                <td>
                  <span v-if="note.EditDate && note.EditDate !== note.CreateDate">
                    {{ formatDate(note.EditDate) }}
                  </span>
                  <span v-else class="muted-text">-</span>
                </td>
                <td>
                  <BaseButton title="Edit" size="small" @clicked="openEditNote(note)" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-else class="no-notes">
          No notes yet
        </p>
      </div>
    </form>

    <div class="action-buttons">
      <BaseButton title="Back to Members" size="small" state="secondary" @clicked="goBack" />
      <BaseButton title="Add Note" size="small" @clicked="openAddNote" />
    </div>
  </section>
</template>

<style scoped>
.error {
  color: #c62828;
  padding: 0.75rem;
  background: #fee;
  border-radius: 0.375rem;
  margin-bottom: 1rem;
}

input{
  max-width: 720px;
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 12px 0;
}

.form-row-checkbox {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
  margin: 12px 0;
}

.form-row-checkbox input[type="checkbox"] {
  width: 1.25rem;
  height: 1.25rem;
  cursor: pointer;
}

.form-row-checkbox label {
  cursor: pointer;
  margin: 0;
  font-weight: 500;
}

.experience-container {
  padding: 0.75rem;
  background: var(--card, #fff);
  border: 1px solid var(--border, #e5e7eb);
  border-radius: 0.5rem;
}

.no-experience {
  color: var(--muted, #6b7280);
  font-style: italic;
  margin: 0;
}

.notes-header {
  font-size: 1.25rem;
  font-weight: 600;
  margin: 0 0 1rem 0;
}

.loading-message {
  color: var(--muted, #6b7280);
  font-style: italic;
  padding: 1rem;
}

.notes-table-container {
  overflow-x: auto;
  border: 1px solid var(--border, #e5e7eb);
  border-radius: 0.5rem;
}

.notes-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.notes-table thead {
  background: var(--card, #fff);
  border-bottom: 2px solid var(--border, #e5e7eb);
}

.notes-table th {
  padding: 0.75rem;
  text-align: left;
  font-weight: 600;
  color: var(--fg, #111827);
  border-bottom: 2px solid var(--border, #e5e7eb);
  white-space: nowrap;
}

.notes-table td {
  padding: 0.75rem;
  border-bottom: 1px solid var(--border, #e5e7eb);
  color: var(--fg, #111827);
}

.notes-table tbody tr:last-child td {
  border-bottom: none;
}

.notes-table tbody tr:hover {
  background: rgba(0, 0, 0, 0.02);
}

[data-theme="dark"] .notes-table tbody tr:hover {
  background: rgba(255, 255, 255, 0.05);
}

.note-content {
  max-width: 400px;
  word-wrap: break-word;
}

.muted-text {
  color: var(--muted, #6b7280);
  font-style: italic;
}

.no-notes {
  color: var(--muted, #6b7280);
  font-style: italic;
  padding: 1rem;
  margin: 0;
}

.action-buttons {
  display: flex;
  gap: 0.75rem;
  margin-top: 1.5rem;
}
</style>
