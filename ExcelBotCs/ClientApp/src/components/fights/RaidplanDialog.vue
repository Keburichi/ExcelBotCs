<script lang="ts" setup>
import type { Fight, Resource } from '@/features/fights/fights.types'
import { computed, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import ResourceCard from '@/components/fights/ResourceCard.vue'
import ResourceEditDialog from '@/components/fights/ResourceEditDialog.vue'
import { useAuth } from '@/composables/useAuth'
import { useResources } from '@/composables/useResources'

const props = defineProps<{
  fight: Fight | null
  isOpen: boolean
}>()

const emit = defineEmits<{
  (e: 'update:isOpen', value: boolean): void
  (e: 'close'): void
}>()

const { user, isAdmin, isMember } = useAuth()

const showEditDialog = ref(false)
const editingResource = ref<Resource | null>(null)

const resourcesComposable = computed(() => {
  if (!props.fight?.Id)
    return null
  return useResources(props.fight.Id)
})

const resources = computed(() => resourcesComposable.value?.resources.value ?? [])
const loading = computed(() => resourcesComposable.value?.loading.value ?? false)
const error = computed(() => resourcesComposable.value?.error.value ?? '')

// Load resources when dialog opens or fight changes
watch(() => [props.isOpen, props.fight?.Id], ([isOpen, fightId]) => {
  if (isOpen && fightId && resourcesComposable.value) {
    resourcesComposable.value.load()
  }
}, { immediate: true })

function canEditResource(resource: Resource): boolean {
  if (!user.value)
    return false
  return !!isAdmin.value || resource.AuthorId === user.value.Id
}

function canDeleteResource(resource: Resource): boolean {
  if (!user.value)
    return false
  return !!isAdmin.value || resource.AuthorId === user.value.Id
}

function handleCreateNew() {
  editingResource.value = null
  showEditDialog.value = true
}

function handleEdit(resource: Resource) {
  editingResource.value = resource
  showEditDialog.value = true
}

async function handleSave(resource: Omit<Resource, 'Id' | 'FightId' | 'AuthorId'>) {
  if (!resourcesComposable.value)
    return

  try {
    if (editingResource.value?.Id) {
      await resourcesComposable.value.update(editingResource.value.Id, resource)
    }
    else {
      await resourcesComposable.value.create(resource)
    }
    showEditDialog.value = false
    editingResource.value = null
  }
  catch (err) {
    console.error('Failed to save resource:', err)
  }
}

async function handleDelete(resource: Resource) {
  if (!resourcesComposable.value || !resource.Id)
    return

  if (confirm(`Are you sure you want to delete "${resource.Name}"?`)) {
    try {
      await resourcesComposable.value.remove(resource.Id)
    }
    catch (err) {
      console.error('Failed to delete resource:', err)
    }
  }
}

function closeDialog() {
  emit('update:isOpen', false)
  emit('close')
}
</script>

<template>
  <BaseModal
    :model-value="isOpen"
    :title="`Resources for ${fight?.Name ?? 'Fight'}`"
    size="large"
    @close="closeDialog"
    @update:model-value="(v) => emit('update:isOpen', v)"
  >
    <template #body>
      <div class="resource-dialog">
        <!-- Header with create button -->
        <div v-if="isMember" class="resource-header">
          <BaseButton state="primary" title="+ Add Resource" @clicked="handleCreateNew" />
        </div>

        <!-- Loading state -->
        <div v-if="loading" class="resource-loading">
          Loading resources...
        </div>

        <!-- Error state -->
        <div v-else-if="error" class="resource-error">
          {{ error }}
        </div>

        <!-- Empty state -->
        <div v-else-if="resources.length === 0" class="resource-empty">
          <p>No resources yet.</p>
          <p v-if="isMember" class="resource-empty-hint">
            Click "Add Resource" to share a raidplan, video guide, macro, or any useful link!
          </p>
        </div>

        <!-- Resources grid -->
        <div v-else class="resource-grid">
          <ResourceCard
            v-for="resource in resources"
            :key="resource.Id"
            :can-delete="canDeleteResource(resource)"
            :can-edit="canEditResource(resource)"
            :resource="resource"
            @delete="handleDelete"
            @edit="handleEdit"
          />
        </div>
      </div>
    </template>
  </BaseModal>

  <!-- Edit/Create Dialog -->
  <ResourceEditDialog
    v-model:is-open="showEditDialog"
    :resource="editingResource"
    @close="showEditDialog = false"
    @save="handleSave"
  />
</template>

<style scoped>
.resource-dialog {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  min-height: 300px;
}

.resource-header {
  display: flex;
  justify-content: flex-end;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--border);
}

.resource-loading,
.resource-error,
.resource-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 200px;
  text-align: center;
  color: var(--muted);
}

.resource-error {
  color: var(--danger);
}

.resource-empty-hint {
  font-size: 0.875rem;
  margin-top: 0.5rem;
}

.resource-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

@media (max-width: 768px) {
  .resource-grid {
    grid-template-columns: 1fr;
  }
}
</style>
