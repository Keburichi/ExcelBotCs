<script lang="ts" setup>
import { onMounted } from 'vue'
import { useCache } from '@/composables/useCache'

const cache = useCache()

onMounted(cache.loadStatus)

function formatDate(dateStr: string | null): string {
  if (!dateStr) return '—'
  const d = new Date(dateStr)
  return d.toLocaleString()
}
</script>

<template>
  <section class="cache-view">
    <h2 class="section-heading">
      Cache Management
    </h2>

    <div v-if="cache.error.value" class="error-banner">
      {{ cache.error.value }}
    </div>

    <!-- Global Actions -->
    <div class="global-actions">
      <button class="action-btn action-btn--fill" @click="cache.fillAll()">
        Fill All
      </button>
      <button class="action-btn action-btn--clear" @click="cache.clearAll()">
        Clear All
      </button>
    </div>

    <!-- Status Table -->
    <div class="table-wrap">
      <div v-if="cache.loading.value" class="loading">
        Loading cache status...
      </div>
      <table v-else-if="cache.status.value" class="cache-table">
        <thead>
          <tr>
            <th>Entity Type</th>
            <th>Count</th>
            <th>Last Refreshed</th>
            <th>Max Modified</th>
            <th>Populated</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="entity in cache.status.value.Entities"
            :key="entity.EntityType"
            :class="{ 'row--selected': cache.selectedEntityType.value === entity.EntityType }"
          >
            <td class="entity-type">
              {{ entity.EntityType }}
            </td>
            <td>{{ entity.Count }}</td>
            <td>{{ formatDate(entity.LastRefreshed) }}</td>
            <td>{{ formatDate(entity.MaxDateModified) }}</td>
            <td>
              <span :class="entity.IsPopulated ? 'badge--ok' : 'badge--empty'" class="badge">
                {{ entity.IsPopulated ? 'Yes' : 'No' }}
              </span>
            </td>
            <td class="actions-cell">
              <button class="action-btn action-btn--sm action-btn--fill" @click="cache.fillCache(entity.EntityType)">
                Fill
              </button>
              <button class="action-btn action-btn--sm action-btn--clear" @click="cache.clearCache(entity.EntityType)">
                Clear
              </button>
              <button class="action-btn action-btn--sm action-btn--view" @click="cache.loadEntities(entity.EntityType)">
                View
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Entity Browser -->
    <div v-if="cache.selectedEntityType.value" class="entity-browser">
      <h3 class="browser-heading">
        Cached {{ cache.selectedEntityType.value }} Entities
        <span class="entity-count">({{ cache.entities.value.length }})</span>
      </h3>

      <div v-if="cache.entitiesLoading.value" class="loading">
        Loading entities...
      </div>
      <div v-else-if="cache.entities.value.length === 0" class="empty-state">
        No cached entities.
      </div>
      <div v-else class="entities-grid">
        <details v-for="(entity, idx) in cache.entities.value" :key="entity.Id || idx" class="entity-card">
          <summary class="entity-summary">
            <span class="entity-id">{{ entity.Id }}</span>
            <span v-if="entity.Name" class="entity-name">{{ entity.Name }}</span>
            <span v-else-if="entity.DiscordName" class="entity-name">{{ entity.DiscordName }}</span>
          </summary>
          <pre class="entity-json">{{ JSON.stringify(entity, null, 2) }}</pre>
        </details>
      </div>
    </div>
  </section>
</template>

<style scoped>
.cache-view {
  width: 100%;
}

.section-heading {
  font-size: 1.875rem;
  font-weight: 700;
  color: var(--fg);
  margin-bottom: 1.5rem;
}

.error-banner {
  background: rgba(239, 68, 68, 0.1);
  border: 1px solid rgba(239, 68, 68, 0.3);
  color: #ef4444;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.global-actions {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
}

.table-wrap {
  background: var(--card-bg, rgba(255, 255, 255, 0.7));
  backdrop-filter: blur(20px);
  border-radius: 16px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  overflow: hidden;
  margin-bottom: 2rem;
}

.cache-table {
  width: 100%;
  border-collapse: collapse;
}

.cache-table th {
  text-align: left;
  padding: 0.75rem 1rem;
  font-weight: 600;
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--muted);
  border-bottom: 1px solid var(--border);
}

.cache-table td {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--border);
  font-size: 0.9rem;
}

.cache-table tr:last-child td {
  border-bottom: none;
}

.row--selected {
  background: rgba(37, 99, 235, 0.05);
}

.entity-type {
  font-weight: 600;
}

.badge {
  display: inline-block;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
}

.badge--ok {
  background: rgba(34, 197, 94, 0.15);
  color: #16a34a;
}

.badge--empty {
  background: rgba(156, 163, 175, 0.15);
  color: #6b7280;
}

.actions-cell {
  display: flex;
  gap: 0.5rem;
}

.action-btn {
  padding: 0.5rem 1rem;
  border-radius: 8px;
  font-weight: 500;
  font-size: 0.85rem;
  border: none;
  cursor: pointer;
  transition: all 0.2s ease;
}

.action-btn--sm {
  padding: 0.3rem 0.6rem;
  font-size: 0.8rem;
}

.action-btn--fill {
  background: #2563eb;
  color: #fff;
}
.action-btn--fill:hover {
  background: #1d4ed8;
  box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.3);
}

.action-btn--clear {
  background: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}
.action-btn--clear:hover {
  background: rgba(239, 68, 68, 0.2);
}

.action-btn--view {
  background: var(--muted-bg);
  color: var(--fg);
}
.action-btn--view:hover {
  background: var(--border);
}

.entity-browser {
  margin-top: 2rem;
}

.browser-heading {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--fg);
  margin-bottom: 1rem;
}

.entity-count {
  font-weight: 400;
  color: var(--muted);
  font-size: 0.9rem;
}

.loading {
  padding: 2rem;
  text-align: center;
  color: var(--muted);
}

.empty-state {
  padding: 2rem;
  text-align: center;
  color: var(--muted);
  background: var(--muted-bg);
  border-radius: 8px;
}

.entities-grid {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.entity-card {
  background: var(--card-bg, rgba(255, 255, 255, 0.7));
  border: 1px solid var(--border);
  border-radius: 8px;
  overflow: hidden;
}

.entity-summary {
  padding: 0.75rem 1rem;
  cursor: pointer;
  display: flex;
  gap: 1rem;
  align-items: center;
  font-size: 0.9rem;
}

.entity-summary:hover {
  background: var(--muted-bg);
}

.entity-id {
  font-family: monospace;
  font-size: 0.8rem;
  color: var(--muted);
}

.entity-name {
  font-weight: 500;
}

.entity-json {
  padding: 1rem;
  margin: 0;
  background: var(--muted-bg);
  font-size: 0.8rem;
  overflow-x: auto;
  border-top: 1px solid var(--border);
}

@media (prefers-color-scheme: dark) {
  .table-wrap {
    background: rgba(30, 30, 30, 0.7);
  }

  .entity-card {
    background: rgba(30, 30, 30, 0.7);
  }
}
</style>
