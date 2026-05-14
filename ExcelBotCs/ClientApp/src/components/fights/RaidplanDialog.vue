<script lang="ts" setup>
import type { Fight, Raidplan } from '@/features/fights/fights.types'
import { computed, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import RaidplanCard from '@/components/fights/RaidplanCard.vue'
import RaidplanEditDialog from '@/components/fights/RaidplanEditDialog.vue'
import { useAuth } from '@/composables/useAuth'
import { useRaidplans } from '@/composables/useRaidplans'

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
const editingRaidplan = ref<Raidplan | null>(null)

// Initialize composable with empty string, will be updated when fight changes
const raidplansComposable = computed(() => {
  if (!props.fight?.Id)
    return null
  return useRaidplans(props.fight.Id)
})

const raidplans = computed(() => raidplansComposable.value?.raidplans.value ?? [])
const loading = computed(() => raidplansComposable.value?.loading.value ?? false)
const error = computed(() => raidplansComposable.value?.error.value ?? '')

// Load raidplans when dialog opens or fight changes
watch(() => [props.isOpen, props.fight?.Id], ([isOpen, fightId]) => {
  if (isOpen && fightId && raidplansComposable.value) {
    raidplansComposable.value.load()
  }
}, { immediate: true })

function canEditRaidplan(raidplan: Raidplan): boolean {
  if (!user.value)
    return false
  return isAdmin.value || raidplan.AuthorId === user.value.Id
}

function canDeleteRaidplan(): boolean {
  return isAdmin.value
}

function handleCreateNew() {
  editingRaidplan.value = null
  showEditDialog.value = true
}

function handleEdit(raidplan: Raidplan) {
  editingRaidplan.value = raidplan
  showEditDialog.value = true
}

async function handleSave(raidplan: Raidplan) {
  if (!raidplansComposable.value)
    return

  try {
    if (editingRaidplan.value?.Id) {
      // Update existing
      await raidplansComposable.value.update(editingRaidplan.value.Id, raidplan)
    }
    else {
      // Create new
      await raidplansComposable.value.create(raidplan)
    }
    showEditDialog.value = false
    editingRaidplan.value = null
  }
  catch (err) {
    console.error('Failed to save raidplan:', err)
  }
}

async function handleDelete(raidplan: Raidplan) {
  if (!raidplansComposable.value || !raidplan.Id)
    return

  if (confirm(`Are you sure you want to delete "${raidplan.Name}"?`)) {
    try {
      await raidplansComposable.value.remove(raidplan.Id)
    }
    catch (err) {
      console.error('Failed to delete raidplan:', err)
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
    :title="`Raidplans for ${fight?.Name ?? 'Fight'}`"
    size="large"
    @close="closeDialog"
    @update:model-value="(v) => emit('update:isOpen', v)"
  >
    <template #body>
      <div class="raidplan-dialog">
        <!-- Header with create button -->
        <div v-if="isMember" class="raidplan-header">
          <BaseButton state="primary" title="+ Create Raidplan" @clicked="handleCreateNew" />
        </div>

        <!-- Loading state -->
        <div v-if="loading" class="raidplan-loading">
          Loading raidplans...
        </div>

        <!-- Error state -->
        <div v-else-if="error" class="raidplan-error">
          {{ error }}
        </div>

        <!-- Empty state -->
        <div v-else-if="raidplans.length === 0" class="raidplan-empty">
          <p>No raidplans yet.</p>
          <p v-if="isMember" class="raidplan-empty-hint">
            Click "Create Raidplan" to add the first one!
          </p>
        </div>

        <!-- Raidplans grid -->
        <div v-else class="raidplan-grid">
          <RaidplanCard
            v-for="raidplan in raidplans"
            :key="raidplan.Id"
            :can-delete="canDeleteRaidplan()"
            :can-edit="canEditRaidplan(raidplan)"
            :raidplan="raidplan"
            @delete="handleDelete"
            @edit="handleEdit"
          />
        </div>
      </div>
    </template>
  </BaseModal>

  <!-- Edit/Create Dialog -->
  <RaidplanEditDialog
    v-model:is-open="showEditDialog"
    :raidplan="editingRaidplan"
    @close="showEditDialog = false"
    @save="handleSave"
  />
</template>

<style scoped>
.raidplan-dialog {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  min-height: 300px;
}

.raidplan-header {
  display: flex;
  justify-content: flex-end;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--border);
}

.raidplan-loading,
.raidplan-error,
.raidplan-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 200px;
  text-align: center;
  color: var(--muted);
}

.raidplan-error {
  color: var(--danger);
}

.raidplan-empty-hint {
  font-size: 0.875rem;
  margin-top: 0.5rem;
}

.raidplan-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

@media (max-width: 768px) {
  .raidplan-grid {
    grid-template-columns: 1fr;
  }
}
</style>
