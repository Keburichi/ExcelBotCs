<script setup lang="ts">
import type { MemberNote } from '@/features/members/members.types'
import { computed, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'

const props = defineProps<{
  modelValue: boolean
  note?: MemberNote
  isEdit: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'save': [note: MemberNote]
  'delete': [note: MemberNote]
}>()

const noteText = ref('')
const showDeleteConfirm = ref(false)

// Watch for dialog open/close to initialize note text
watch(() => props.modelValue, (isOpen) => {
  if (isOpen) {
    noteText.value = props.note?.Note || ''
    showDeleteConfirm.value = false
  }
})

const canSave = computed(() => noteText.value.trim().length > 0)

function close() {
  emit('update:modelValue', false)
}

function save() {
  if (!canSave.value)
    return

  const savedNote: MemberNote = props.isEdit && props.note
    ? { ...props.note, Note: noteText.value.trim() }
    : { Note: noteText.value.trim() }

  emit('save', savedNote)
  close()
}

function confirmDelete() {
  showDeleteConfirm.value = true
}

function deleteNote() {
  if (props.note) {
    emit('delete', props.note)
    close()
  }
}

function cancelDelete() {
  showDeleteConfirm.value = false
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
</script>

<template>
  <div v-if="modelValue" class="dialog-overlay" @click.self="close">
    <div class="dialog">
      <div class="dialog__header">
        <h2>{{ isEdit ? 'Edit Note' : 'Add Note' }}</h2>
        <button class="dialog__close" @click="close">
          ×
        </button>
      </div>

      <div v-if="!showDeleteConfirm" class="dialog__body">
        <div v-if="isEdit && note" class="note-metadata">
          <div class="metadata-item">
            <span class="metadata-label">Author:</span>
            <span class="metadata-value">{{ note.Author || 'Unknown' }}</span>
          </div>
          <div class="metadata-item">
            <span class="metadata-label">Created:</span>
            <span class="metadata-value">{{ formatDate(note.CreateDate) }}</span>
          </div>
          <div v-if="note.EditDate && note.EditDate !== note.CreateDate" class="metadata-item">
            <span class="metadata-label">Last edited:</span>
            <span class="metadata-value">{{ formatDate(note.EditDate) }}</span>
          </div>
        </div>

        <div class="form-group">
          <label for="note-text">Note</label>
          <textarea
            id="note-text"
            v-model="noteText"
            rows="6"
            placeholder="Enter note text..."
            autofocus
          />
        </div>
      </div>

      <div v-else class="dialog__body">
        <div class="delete-confirmation">
          <p class="delete-warning">
            Are you sure you want to delete this note?
          </p>
          <p class="delete-details">
            This action cannot be undone.
          </p>
        </div>
      </div>

      <div class="dialog__footer">
        <template v-if="!showDeleteConfirm">
          <div class="footer-left">
            <BaseButton
              v-if="isEdit"
              title="Delete"
              size="small"
              state="danger"
              @clicked="confirmDelete"
            />
          </div>
          <div class="footer-right">
            <BaseButton title="Cancel" size="small" state="secondary" @clicked="close" />
            <BaseButton
              title="Save"
              size="small"
              :disabled="!canSave"
              @clicked="save"
            />
          </div>
        </template>
        <template v-else>
          <div class="footer-right footer-right--full">
            <BaseButton title="Cancel" size="small" state="secondary" @clicked="cancelDelete" />
            <BaseButton title="Delete" size="small" state="danger" @clicked="deleteNote" />
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.dialog {
  background: var(--card, #fff);
  border-radius: 0.5rem;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
}

.dialog__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.5rem;
  border-bottom: 1px solid var(--border, #e5e7eb);
}

.dialog__header h2 {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 600;
}

.dialog__close {
  background: none;
  border: none;
  font-size: 2rem;
  line-height: 1;
  cursor: pointer;
  color: var(--muted, #6b7280);
  padding: 0;
  width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.25rem;
  transition: background-color 0.2s;
}

.dialog__close:hover {
  background: var(--border, #e5e7eb);
}

.dialog__body {
  padding: 1.5rem;
  flex: 1;
  overflow-y: auto;
}

.note-metadata {
  background: rgba(0, 0, 0, 0.02);
  border: 1px solid var(--border, #e5e7eb);
  border-radius: 0.375rem;
  padding: 1rem;
  margin-bottom: 1.5rem;
}

[data-theme="dark"] .note-metadata {
  background: rgba(255, 255, 255, 0.05);
}

.metadata-item {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.metadata-item:last-child {
  margin-bottom: 0;
}

.metadata-label {
  font-weight: 600;
  color: var(--muted, #6b7280);
  min-width: 100px;
}

.metadata-value {
  color: var(--fg, #111827);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-weight: 500;
  color: var(--fg, #111827);
}

.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--border, #d1d5db);
  border-radius: 0.375rem;
  font-family: inherit;
  font-size: 0.875rem;
  resize: vertical;
  min-height: 120px;
  background: var(--card, #fff);
  color: var(--fg, #111827);
}

.form-group textarea:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.delete-confirmation {
  text-align: center;
  padding: 2rem 0;
}

.delete-warning {
  font-size: 1.125rem;
  font-weight: 600;
  color: #dc2626;
  margin: 0 0 0.5rem 0;
}

.delete-details {
  color: var(--muted, #6b7280);
  margin: 0;
}

.dialog__footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  border-top: 1px solid var(--border, #e5e7eb);
}

.footer-left {
  display: flex;
  gap: 0.5rem;
}

.footer-right {
  display: flex;
  gap: 0.5rem;
  margin-left: auto;
}

.footer-right--full {
  width: 100%;
  justify-content: flex-end;
}
</style>
