<script lang="ts" setup>
import type { Raidplan } from '@/features/fights/fights.types'
import BaseButton from '@/components/BaseButton.vue'
import BaseCard from '@/components/BaseCard.vue'
import { useAuth } from '@/composables/useAuth'

const props = defineProps<{
  raidplan: Raidplan
  canEdit: boolean
  canDelete: boolean
}>()

const emit = defineEmits<{
  (e: 'edit', raidplan: Raidplan): void
  (e: 'delete', raidplan: Raidplan): void
}>()

const { user } = useAuth()

function openUrl() {
  if (props.raidplan.Url) {
    window.open(props.raidplan.Url, '_blank', 'noopener,noreferrer')
  }
}
</script>

<template>
  <BaseCard :title="raidplan.Name" variant="elevated">
    <template #body>
      <p class="raidplan-description">
        {{ raidplan.Description }}
      </p>
      <a
        v-if="raidplan.Url"
        :href="raidplan.Url"
        class="raidplan-link"
        rel="noopener noreferrer"
        target="_blank"
        @click.stop
      >
        <svg
          fill="none" height="16" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
          stroke-width="2" viewBox="0 0 24 24" width="16" xmlns="http://www.w3.org/2000/svg"
        >
          <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
          <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
        </svg>
        Open Raidplan
      </a>
    </template>

    <template #footer>
      <div class="raidplan-actions">
        <BaseButton
          v-if="canEdit"
          size="small"
          state="secondary"
          title="Edit"
          @clicked="emit('edit', raidplan)"
        />
        <BaseButton
          v-if="canDelete"
          size="small"
          state="danger"
          title="Delete"
          @clicked="emit('delete', raidplan)"
        />
      </div>
    </template>
  </BaseCard>
</template>

<style scoped>
.raidplan-description {
  margin-bottom: 1rem;
  color: var(--fg);
  line-height: 1.5;
}

.raidplan-link {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--link);
  text-decoration: none;
  font-size: 0.875rem;
  transition: opacity 0.2s;
}

.raidplan-link:hover {
  opacity: 0.8;
  text-decoration: underline;
}

.raidplan-actions {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
}
</style>
