<script lang="ts" setup>
import type { ArchiveSearchParams, FCEvent } from '@/features/events/events.types'
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import CardList from '@/components/CardList.vue'
import EventCard from '@/components/events/EventCard.vue'
import SelectMenu from '@/components/SelectMenu.vue'
import { useAuth } from '@/composables/useAuth'
import { useEvents } from '@/composables/useEvents'
import { EventType, eventTypeToString } from '@/features/events/events.types'

const router = useRouter()
const e = useEvents()
const { isAdmin } = useAuth()

// Search and filter state
const searchText = ref('')
const selectedEventType = ref<EventType | null>(null)
const startDate = ref('')
const endDate = ref('')

// Page size options
const pageSizeOptions = [
  { label: '10', value: 10 },
  { label: '20', value: 20 },
  { label: '50', value: 50 },
]

// Event type options for filter
const eventTypeOptions = computed(() => [
  { label: 'All', value: null },
  ...Object.values(EventType)
    .filter(v => typeof v === 'number')
    .map(type => ({
      label: eventTypeToString(type as EventType),
      value: type as EventType,
    })),
])

// Build search params from filters
const searchParams = computed<ArchiveSearchParams>(() => ({
  searchText: searchText.value || undefined,
  startDate: startDate.value || undefined,
  endDate: endDate.value || undefined,
  eventType: selectedEventType.value ?? undefined,
}))

const totalPages = computed(() =>
  Math.max(1, Math.ceil(e.archiveTotalCount.value / e.archivePageSize.value)),
)

// Debounced search — resets to page 1
let searchTimeout: number | null = null

function debouncedSearch() {
  if (searchTimeout)
    clearTimeout(searchTimeout)
  searchTimeout = window.setTimeout(() => {
    e.loadArchived(searchParams.value, 1)
  }, 300)
}

// Watch for filter changes
watch([searchText, selectedEventType, startDate, endDate], () => {
  debouncedSearch()
})

function goToPage(page: number) {
  e.loadArchived(searchParams.value, page)
}

function changePageSize(size: number) {
  e.loadArchived(searchParams.value, 1, size)
}

function goBackToEvents() {
  router.push({ name: 'events' })
}

function goEdit(event: FCEvent) {
  router.push({ name: 'event-edit', params: { id: event.Id } })
}

async function handleRestore(event: FCEvent) {
  const success = await e.restoreEvent(event.Id)
  if (success) {
    await e.loadArchived(searchParams.value)
  }
}

// Get total signups across all occurrences
function getTotalSignups(event: FCEvent): number {
  return event.Signups?.length ?? 0
}

// Get total participants across all groups
function getTotalParticipants(event: FCEvent): number {
  if (!event.Groups || event.Groups.length === 0)
    return 0
  return event.Groups.reduce((total, group) => {
    return total + (group.Participants?.length ?? 0)
  }, 0)
}

// Format archived date
function formatArchivedDate(dateString?: string): string {
  if (!dateString)
    return 'Unknown'
  return new Date(dateString).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

// Get last occurrence date
function getLastOccurrenceDate(event: FCEvent): string {
  if (!event.Occurrences || event.Occurrences.length === 0)
    return 'No occurrences'
  const lastOcc = event.Occurrences.reduce((latest, occ) =>
    new Date(occ.OccurrenceDate) > new Date(latest.OccurrenceDate) ? occ : latest,
  )
  return new Date(lastOcc.OccurrenceDate).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

onMounted(() => {
  e.loadArchived()
})
</script>

<template>
  <section class="home">
    <div class="page-header">
      <div class="header-row">
        <h2 class="page-title">
          Archived Events
        </h2>
        <BaseButton
          state="secondary"
          title="Back to Events"
          @clicked="goBackToEvents"
        />
      </div>
    </div>

    <div class="filters-section">
      <div class="search-row">
        <input
          v-model="searchText"
          class="search-input"
          placeholder="Search by event name..."
          type="text"
        >
      </div>

      <div class="filter-row">
        <div class="filter-group">
          <label class="filter-label">Event Type</label>
          <SelectMenu
            v-model="selectedEventType"
            :options="eventTypeOptions"
            placeholder="All Types"
          />
        </div>

        <div class="filter-group">
          <label class="filter-label">From Date</label>
          <input
            v-model="startDate"
            class="date-input"
            type="date"
          >
        </div>

        <div class="filter-group">
          <label class="filter-label">To Date</label>
          <input
            v-model="endDate"
            class="date-input"
            type="date"
          >
        </div>
      </div>
    </div>

    <p v-if="e.error" class="error">
      {{ e.error }}
    </p>

    <div class="results-bar">
      <p class="results-count">
        {{ e.archiveTotalCount.value }} archived event{{ e.archiveTotalCount.value === 1 ? '' : 's' }} found
      </p>
      <div class="page-size-control">
        <label class="filter-label">Per page</label>
        <select
          :value="e.archivePageSize.value"
          class="page-size-select"
          @change="changePageSize(Number(($event.target as HTMLSelectElement).value))"
        >
          <option v-for="opt in pageSizeOptions" :key="opt.value" :value="opt.value">
            {{ opt.label }}
          </option>
        </select>
      </div>
    </div>

    <div v-if="e.archiveLoading.value" class="loading">
      Loading archived events...
    </div>

    <CardList
      v-else
      :columns="2"
      :items="e.archivedEvents.value"
      item-key="Id"
    >
      <template #item="{ item }">
        <div class="archived-event-card">
          <EventCard
            :fc-event="item"
            :is-admin="isAdmin?.valueOf()"
            :is-archive-view="true"
            :is-member="false"
            @start-edit="goEdit"
          />

          <div class="archive-info">
            <div class="archive-stats">
              <span class="stat">
                <strong>{{ item.Occurrences?.length ?? 0 }}</strong> occurrences
              </span>
              <span class="stat">
                <strong>{{ getTotalSignups(item) }}</strong> total signups
              </span>
              <span class="stat">
                <strong>{{ getTotalParticipants(item) }}</strong> total participants
              </span>
            </div>
            <div class="archive-dates">
              <span class="date-info">
                Last occurrence: {{ getLastOccurrenceDate(item) }}
              </span>
              <span class="date-info">
                Archived: {{ formatArchivedDate(item.ArchivedDate) }}
              </span>
            </div>
            <div class="archive-actions">
              <BaseButton
                v-if="isAdmin"
                state="secondary"
                title="Restore Event"
                tooltip="Restore this event back to active status"
                @clicked="handleRestore(item)"
              />
            </div>
          </div>
        </div>
      </template>
    </CardList>

    <div v-if="totalPages > 1 && !e.archiveLoading.value" class="pagination">
      <button
        class="page-btn"
        :disabled="e.archivePage.value <= 1"
        @click="goToPage(e.archivePage.value - 1)"
      >
        Previous
      </button>
      <span class="page-info">
        Page {{ e.archivePage.value }} of {{ totalPages }}
      </span>
      <button
        class="page-btn"
        :disabled="!e.archiveHasMore.value"
        @click="goToPage(e.archivePage.value + 1)"
      >
        Next
      </button>
    </div>

    <div v-if="!e.archiveLoading.value && e.archivedEvents.value.length === 0" class="empty-state">
      <p>No archived events found.</p>
      <p v-if="searchText || selectedEventType || startDate || endDate" class="empty-hint">
        Try adjusting your search filters.
      </p>
    </div>
  </section>
</template>

<style scoped>
.page-header {
  margin-bottom: 2rem;
}

.header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
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

.filters-section {
  margin-bottom: 1.5rem;
  padding: 1rem;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 0.5rem;
}

.search-row {
  margin-bottom: 1rem;
}

.search-input {
  width: 100%;
  max-width: 400px;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid var(--border);
  border-radius: 0.5rem;
  background: var(--bg);
  color: var(--fg);
  transition: border-color 0.2s, box-shadow 0.2s;
}

.search-input:focus {
  outline: none;
  border-color: var(--link);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.search-input::placeholder {
  color: var(--muted);
}

.filter-row {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  align-items: flex-end;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.filter-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--muted);
}

.date-input {
  padding: 0.5rem 0.75rem;
  font-size: 0.9rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--fg);
}

.date-input:focus {
  outline: none;
  border-color: var(--link);
}

.results-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 1rem 0;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.results-count {
  margin: 0;
  color: var(--muted);
  font-size: 0.875rem;
}

.page-size-control {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.page-size-select {
  padding: 0.375rem 0.5rem;
  font-size: 0.875rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--fg);
}

.page-size-select:focus {
  outline: none;
  border-color: var(--link);
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin: 1.5rem 0;
}

.page-btn {
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  font-weight: 500;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  color: var(--fg);
  cursor: pointer;
  transition: background-color 0.2s, border-color 0.2s;
}

.page-btn:hover:not(:disabled) {
  background: var(--bg);
  border-color: var(--link);
}

.page-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.page-info {
  font-size: 0.875rem;
  color: var(--muted);
}

.loading {
  text-align: center;
  padding: 2rem;
  color: var(--muted);
}

.archived-event-card {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.archive-info {
  padding: 1rem;
  background: var(--muted-bg);
  border: 1px solid var(--border);
  border-top: none;
  border-radius: 0 0 0.5rem 0.5rem;
}

.archive-stats {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.stat {
  font-size: 0.875rem;
  color: var(--fg);
}

.stat strong {
  color: var(--link);
}

.archive-dates {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.date-info {
  font-size: 0.8rem;
  color: var(--muted);
}

.archive-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.empty-state {
  text-align: center;
  padding: 3rem;
  color: var(--muted);
}

.empty-hint {
  font-size: 0.875rem;
  margin-top: 0.5rem;
}

.error {
  color: var(--danger);
  margin-bottom: 1rem;
}
</style>
