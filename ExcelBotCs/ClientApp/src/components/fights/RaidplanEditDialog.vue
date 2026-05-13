<script lang="ts" setup>
import type { Raidplan } from '@/features/fights/fights.types'
import { computed, reactive, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'

const props = defineProps<{
  raidplan?: Raidplan | null
  isOpen: boolean
}>()

const emit = defineEmits<{
  (e: 'update:isOpen', value: boolean): void
  (e: 'save', raidplan: Raidplan): void
  (e: 'close'): void
}>()

const formData = reactive<Raidplan>({
  Name: '',
  Description: '',
  Url: '',
  AuthorId: '',
})

// Watch for changes to the raidplan prop to populate the form
watch(() => props.raidplan, (newRaidplan) => {
  if (newRaidplan) {
    Object.assign(formData, {
      Id: newRaidplan.Id,
      Name: newRaidplan.Name,
      Description: newRaidplan.Description,
      Url: newRaidplan.Url,
      AuthorId: newRaidplan.AuthorId,
    })
  }
  else {
    // Reset form for new raidplan
    Object.assign(formData, {
      Id: undefined,
      Name: '',
      Description: '',
      Url: '',
      AuthorId: '',
    })
  }
}, { immediate: true })

function handleSave() {
  emit('save', { ...formData })
  closeDialog()
}

function closeDialog() {
  emit('update:isOpen', false)
  emit('close')
}

const isEditing = computed(() => !!props.raidplan?.Id)
</script>

<template>
  <BaseModal
    :model-value="isOpen"
    :title="isEditing ? 'Edit Raidplan' : 'Create Raidplan'"
    size="medium"
    @close="closeDialog"
    @update:model-value="(v) => emit('update:isOpen', v)"
  >
    <template #body>
      <form class="raidplan-form" @submit.prevent="handleSave">
        <div class="form-group">
          <label class="form-label" for="name">Name</label>
          <input
            id="name"
            v-model="formData.Name"
            class="form-input"
            placeholder="Enter raidplan name"
            required
            type="text"
          >
        </div>

        <div class="form-group">
          <label class="form-label" for="description">Description</label>
          <textarea
            id="description"
            v-model="formData.Description"
            class="form-input form-textarea"
            placeholder="Enter description"
            required
            rows="4"
          />
        </div>

        <div class="form-group">
          <label class="form-label" for="url">URL</label>
          <input
            id="url"
            v-model="formData.Url"
            class="form-input"
            placeholder="https://example.com"
            required
            type="url"
          >
        </div>
      </form>
    </template>

    <template #actions>
      <BaseButton state="secondary" title="Cancel" @clicked="closeDialog" />
      <BaseButton :title="isEditing ? 'Update' : 'Create'" state="primary" @clicked="handleSave" />
    </template>
  </BaseModal>
</template>

<style scoped>
.raidplan-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-label {
  font-weight: 600;
  font-size: 0.875rem;
  color: var(--fg);
}

.form-input {
  padding: 0.75rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  font-size: 1rem;
  background: var(--card);
  color: var(--fg);
  transition: border-color 0.2s;
}

.form-input:focus {
  outline: none;
  border-color: var(--link);
}

.form-textarea {
  resize: vertical;
  min-height: 100px;
}
</style>
