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
const saving = ref(false)
const error = ref('')
const successMessage = ref('')
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

async function handleSave() {
  saving.value = true
  error.value = ''
  successMessage.value = ''
  try {
    await MembersApi.update(form.Id!, form)
    successMessage.value = 'Member updated successfully'
    setTimeout(() => successMessage.value = '', 3000)
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to save member'
  }
  finally {
    saving.value = false
  }
}

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
  <section class="page member-edit">
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

    <div class="page-header">
      <h2 class="page-title">
        Edit {{ form.DiscordName }}
      </h2>
    </div>
    <p v-if="error" class="error">
      {{ error }}
    </p>
    <p v-if="successMessage" class="success">
      {{ successMessage }}
    </p>

    <div class="member-form">
      <!-- Basic Information Section -->
      <section class="form-section">
        <h3 class="section-header">
          Basic Information
        </h3>
        <div class="form-row">
          <label>Character Id</label>
          <input v-model="form.LodestoneId" placeholder="Character Lodestone Id" type="text">
        </div>

        <div class="form-row">
          <label>Lodestone Verification Token</label>
          <input
            v-model="form.LodestoneVerificationToken" disabled placeholder="Lodestone verification token"
            type="text"
          >
        </div>

        <div class="form-row-checkbox">
          <input
            :id="form.DiscordId" v-model="form.Subbed" :name="form.DiscordId" placeholder="Is player subbed?"
            type="checkbox"
          >
          <label :for="form.DiscordId">Subbed?</label>
        </div>

        <div class="section-actions">
          <BaseButton
            :disabled="saving"
            :title="saving ? 'Saving...' : 'Save Changes'"
            @clicked="handleSave"
          />
        </div>
      </section>

      <!-- Experience Section -->
      <section class="form-section">
        <h3 class="section-header">
          Experience
        </h3>
        <div v-if="form.Experience?.length" class="experience-container">
          <ExperienceTags :experience="form.Experience" />
        </div>
        <p v-else class="no-experience">
          No cleared content yet
        </p>
      </section>

      <!-- Notes Section -->
      <section class="form-section">
        <div class="notes-header-row">
          <h3 class="section-header">
            Notes
          </h3>
          <BaseButton size="small" title="Add Note" @clicked="openAddNote" />
        </div>
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
      </section>

      <!-- Actions -->
      <div class="action-buttons">
        <BaseButton state="secondary" title="Back to Members" @clicked="goBack" />
      </div>
    </div>
  </section>
</template>

<style scoped>
.page {
  max-width: 800px;
  margin: 0 auto;
}

/* Page header */
.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  color: var(--fg);
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.02em;
}

/* Error message */
.error {
  padding: 1rem;
  background: var(--alert-error-bg, rgba(220, 38, 38, 0.1));
  color: var(--alert-error-fg, #c62828);
  border: 1px solid var(--alert-error-border, rgba(220, 38, 38, 0.3));
  border-radius: 12px;
  margin-bottom: 1.5rem;
}

/* Success message */
.success {
  padding: 1rem;
  background: rgba(34, 197, 94, 0.1);
  color: #16a34a;
  border: 1px solid rgba(34, 197, 94, 0.3);
  border-radius: 12px;
  margin-bottom: 1.5rem;
}

:root[data-theme='dark'] .success {
  background: rgba(34, 197, 94, 0.15);
  color: #4ade80;
  border-color: rgba(34, 197, 94, 0.4);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .success {
    background: rgba(34, 197, 94, 0.15);
    color: #4ade80;
    border-color: rgba(34, 197, 94, 0.4);
  }
}

.member-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Section containers with glassmorphism */
.form-section {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 16px;
  padding: 1.5rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

:root[data-theme='dark'] .form-section {
  background: rgba(18, 26, 45, 0.7);
  border: 2px solid rgba(255, 255, 255, 0.15);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .form-section {
    background: rgba(18, 26, 45, 0.7);
    border: 2px solid rgba(255, 255, 255, 0.15);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.form-section:hover {
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 6px 20px rgba(59, 130, 246, 0.12),
  0 4px 16px rgba(0, 0, 0, 0.1),
  inset 0 1px 0 rgba(255, 255, 255, 0.6);
}

:root[data-theme='dark'] .form-section:hover {
  border-color: rgba(59, 130, 246, 0.5);
  box-shadow: 0 6px 20px rgba(59, 130, 246, 0.2),
  0 4px 16px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .form-section:hover {
    border-color: rgba(59, 130, 246, 0.5);
    box-shadow: 0 6px 20px rgba(59, 130, 246, 0.2),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
}

/* Section headers */
.section-header {
  margin: 0 0 1.25rem 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--fg);
  padding-bottom: 0.75rem;
  border-bottom: 1px solid rgba(var(--color-border), 0.3);
}

.notes-header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.notes-header-row .section-header {
  margin: 0;
  padding: 0;
  border: none;
  flex: 1;
}

/* Form rows */
.form-row {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.form-row:last-child {
  margin-bottom: 0;
}

.form-row-checkbox {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.form-row-checkbox:last-child {
  margin-bottom: 0;
}

.section-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid rgba(var(--color-border), 0.2);
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
  color: var(--fg);
}

/* Labels */
label {
  font-weight: 500;
  font-size: 0.9rem;
  color: var(--fg);
}

/* Experience */
.experience-container {
  padding: 0.75rem;
  background: rgba(var(--color-card), 0.5);
  border: 1px solid rgba(var(--color-border), 0.3);
  border-radius: 12px;
}

.no-experience {
  color: var(--muted);
  font-style: italic;
  margin: 0;
  padding: 1rem;
  text-align: center;
}

/* Notes table */
.loading-message {
  color: var(--muted);
  font-style: italic;
  padding: 1rem;
  text-align: center;
}

.notes-table-container {
  overflow-x: auto;
  border: 1px solid rgba(var(--color-border), 0.3);
  border-radius: 12px;
  background: rgba(var(--color-card), 0.3);
}

.notes-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}

.notes-table thead {
  background: rgba(var(--color-card), 0.5);
  border-bottom: 2px solid rgba(var(--color-border), 0.5);
}

.notes-table th {
  padding: 0.75rem;
  text-align: left;
  font-weight: 600;
  color: var(--fg);
  border-bottom: 2px solid rgba(var(--color-border), 0.5);
  white-space: nowrap;
}

.notes-table td {
  padding: 0.75rem;
  border-bottom: 1px solid rgba(var(--color-border), 0.3);
  color: var(--fg);
}

.notes-table tbody tr:last-child td {
  border-bottom: none;
}

.notes-table tbody tr:hover {
  background: rgba(59, 130, 246, 0.05);
}

:root[data-theme='dark'] .notes-table tbody tr:hover {
  background: rgba(59, 130, 246, 0.1);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .notes-table tbody tr:hover {
    background: rgba(59, 130, 246, 0.1);
  }
}

.note-content {
  max-width: 400px;
  word-wrap: break-word;
}

.muted-text {
  color: var(--muted);
  font-style: italic;
}

.no-notes {
  color: var(--muted);
  font-style: italic;
  padding: 1rem;
  margin: 0;
  text-align: center;
}

/* Actions */
.action-buttons {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  padding-top: 1rem;
}

/* Responsive adjustments */
@media (max-width: 768px) {
  .page {
    max-width: 100%;
  }

  .form-section {
    padding: 1.25rem;
  }

  .notes-header-row {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .notes-table-container {
    font-size: 0.8rem;
  }

  .notes-table th,
  .notes-table td {
    padding: 0.5rem;
  }
}

@media (max-width: 480px) {
  .form-section {
    padding: 1rem;
  }

  .section-header {
    font-size: 1rem;
  }

  .action-buttons {
    flex-direction: column;
  }
}
</style>
