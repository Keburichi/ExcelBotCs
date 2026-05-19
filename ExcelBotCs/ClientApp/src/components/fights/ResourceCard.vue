<script lang="ts" setup>
import type { Resource } from '@/features/fights/fights.types'
import { ResourceTypeLabels } from '@/features/fights/fights.types'
import BaseButton from '@/components/BaseButton.vue'
import BaseCard from '@/components/BaseCard.vue'

const props = defineProps<{
  resource: Resource
  canEdit: boolean
  canDelete: boolean
}>()

const emit = defineEmits<{
  (e: 'edit', resource: Resource): void
  (e: 'delete', resource: Resource): void
}>()

const typeLabel = ResourceTypeLabels[props.resource.Type] ?? 'Link'
</script>

<template>
  <BaseCard :title="resource.Name" variant="elevated">
    <template #body>
      <span class="resource-type-badge">{{ typeLabel }}</span>
      <p v-if="resource.Description" class="resource-description">
        {{ resource.Description }}
      </p>
      <a
        v-if="resource.Url"
        :href="resource.Url"
        class="resource-link"
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
        Open {{ typeLabel }}
      </a>
    </template>

    <template #footer>
      <div class="resource-actions">
        <BaseButton
          v-if="canEdit"
          size="small"
          state="secondary"
          title="Edit"
          @clicked="emit('edit', resource)"
        />
        <BaseButton
          v-if="canDelete"
          size="small"
          state="danger"
          title="Delete"
          @clicked="emit('delete', resource)"
        />
      </div>
    </template>
  </BaseCard>
</template>

<style scoped>
.resource-type-badge {
  display: inline-block;
  padding: 0.2rem 0.6rem;
  margin-bottom: 0.75rem;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.025em;
  border-radius: 4px;
  background: color-mix(in srgb, var(--link) 15%, transparent);
  color: var(--link);
}

.resource-description {
  margin-bottom: 1rem;
  color: var(--fg);
  line-height: 1.5;
}

.resource-link {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--link);
  text-decoration: none;
  font-size: 0.875rem;
  transition: opacity 0.2s;
}

.resource-link:hover {
  opacity: 0.8;
  text-decoration: underline;
}

.resource-actions {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
}
</style>
