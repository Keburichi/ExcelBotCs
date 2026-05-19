<script lang="ts" setup>
import type { Resource } from '@/features/fights/fights.types'
import { ResourceType, ResourceTypeLabels } from '@/features/fights/fights.types'
import { computed, reactive, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'

const props = defineProps<{
  resource?: Resource | null
  isOpen: boolean
}>()

const emit = defineEmits<{
  (e: 'update:isOpen', value: boolean): void
  (e: 'save', resource: Omit<Resource, 'Id' | 'FightId' | 'AuthorId'>): void
  (e: 'close'): void
}>()

const formData = reactive({
  Name: '',
  Description: '',
  Url: '',
  Type: ResourceType.Raidplan as ResourceType,
})

const resourceTypeOptions = Object.entries(ResourceTypeLabels).map(([value, label]) => ({
  value: Number(value) as ResourceType,
  label,
}))

watch(() => props.resource, (newResource) => {
  if (newResource) {
    Object.assign(formData, {
      Name: newResource.Name,
      Description: newResource.Description ?? '',
      Url: newResource.Url,
      Type: newResource.Type,
    })
  }
  else {
    Object.assign(formData, {
      Name: '',
      Description: '',
      Url: '',
      Type: ResourceType.Raidplan,
    })
  }
}, { immediate: true })

const showDiscardConfirm = ref(false)

const isDirty = computed(() => {
  const original = props.resource
  if (original) {
    return formData.Name !== original.Name
      || formData.Description !== (original.Description ?? '')
      || formData.Url !== original.Url
      || formData.Type !== original.Type
  }
  return formData.Name !== '' || formData.Description !== '' || formData.Url !== ''
})

function guardClose(): boolean {
  if (isDirty.value) {
    showDiscardConfirm.value = true
    return false
  }
  return true
}

function handleSave() {
  emit('save', { ...formData })
  forceClose()
}

function forceClose() {
  showDiscardConfirm.value = false
  emit('update:isOpen', false)
  emit('close')
}

function closeDialog() {
  if (!guardClose())
    return
  forceClose()
}

const isEditing = computed(() => !!props.resource?.Id)
</script>

<template>
  <BaseModal
    :model-value="isOpen"
    :title="isEditing ? 'Edit Resource' : 'Add Resource'"
    :close-guard="guardClose"
    size="medium"
    @close="closeDialog"
    @update:model-value="(v) => emit('update:isOpen', v)"
  >
    <template #body>
      <form class="resource-form" @submit.prevent="handleSave">
        <div class="form-group">
          <label class="form-label" for="type">Type</label>
          <select
            id="type"
            v-model.number="formData.Type"
            class="form-input"
          >
            <option
              v-for="opt in resourceTypeOptions"
              :key="opt.value"
              :value="opt.value"
            >
              {{ opt.label }}
            </option>
          </select>
        </div>

        <div class="form-group">
          <label class="form-label" for="name">Name</label>
          <input
            id="name"
            v-model="formData.Name"
            class="form-input"
            placeholder="Enter resource name"
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
            placeholder="Enter description (optional)"
            rows="3"
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

      <div v-if="showDiscardConfirm" class="discard-confirm">
        <p class="discard-confirm__message">
          You have unsaved changes. Do you want to discard them?
        </p>
        <div class="discard-confirm__actions">
          <BaseButton size="small" state="secondary" title="Continue editing" @clicked="showDiscardConfirm = false" />
          <BaseButton size="small" state="danger" title="Discard changes" @clicked="forceClose" />
        </div>
      </div>
    </template>

    <template #actions>
      <BaseButton state="secondary" title="Cancel" @clicked="closeDialog" />
      <BaseButton :title="isEditing ? 'Update' : 'Create'" state="primary" @clicked="handleSave" />
    </template>
  </BaseModal>
</template>

<style scoped>
.resource-form {
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

select.form-input {
  cursor: pointer;
  appearance: auto;
}

.form-textarea {
  resize: vertical;
  min-height: 80px;
}

.discard-confirm {
  margin-top: 1.25rem;
  padding: 1rem;
  border-radius: 0.5rem;
  background: color-mix(in srgb, #ef4444 10%, var(--card));
  border: 1px solid color-mix(in srgb, #ef4444 40%, transparent);
}

.discard-confirm__message {
  margin: 0 0 0.75rem;
  font-size: 0.9rem;
  color: var(--fg);
}

.discard-confirm__actions {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
}
</style>
