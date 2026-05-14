<script setup lang="ts">
import type { Fight } from '@/features/fights/fights.types'
import type { FilterDef } from '@/utils/filters.types'
import { computed, onMounted, ref } from 'vue'
import CardList from '@/components/CardList.vue'
import FightCard from '@/components/fights/FightCard.vue'
import FilterBar from '@/components/FilterBar.vue'
import { useAuth } from '@/composables/useAuth'
import { useFights } from '@/composables/useFights'
import { FightType } from '@/features/fights/fights.types'
import { useFilters } from '@/utils/useFilters'

const f = useFights()
const { isMember } = useAuth()

onMounted(f.getFights)

const searchText = ref('')

// Get unique expansions and zones for filter options
const expansionOptions = computed(() => {
  const expansions = new Set<string>()
  f.fights.value.forEach((fight) => {
    if (fight.FFLogsExpansionName) {
      expansions.add(fight.FFLogsExpansionName)
    }
  })
  return Array.from(expansions)
    .sort()
    .map(exp => ({ label: exp, value: exp }))
})

const zoneOptions = computed(() => {
  const zones = new Set<string>()
  f.fights.value.forEach((fight) => {
    if (fight.FFLogsZoneName) {
      zones.add(fight.FFLogsZoneName)
    }
  })
  return Array.from(zones)
    .sort()
    .map(zone => ({ label: zone, value: zone }))
})

const fightTypeOptions = [
  { label: 'Extreme', value: FightType.Extreme.valueOf() },
  { label: 'Savage', value: FightType.Savage },
  { label: 'Ultimate', value: FightType.Ultimate },
  { label: 'Unreal', value: FightType.Unreal },
]

// Make filters computed so they update when options change
const filters = computed<FilterDef<Fight>[]>(() => [
  {
    id: 'expansion',
    label: 'Expansion',
    multiple: true,
    options: expansionOptions.value,
    predicate: (fight, selected) => {
      const arr = Array.isArray(selected) ? selected : [selected]
      if (arr.length === 0)
        return true
      return fight.FFLogsExpansionName && arr.includes(fight.FFLogsExpansionName)
    },
  },
  {
    id: 'zone',
    label: 'Zone',
    multiple: true,
    options: zoneOptions.value,
    predicate: (fight, selected) => {
      const arr = Array.isArray(selected) ? selected : [selected]
      if (arr.length === 0)
        return true
      return fight.FFLogsZoneName && arr.includes(fight.FFLogsZoneName)
    },
  },
  {
    id: 'type',
    label: 'Difficulty',
    multiple: true,
    options: fightTypeOptions,
    predicate: (fight, selected) => {
      const arr = Array.isArray(selected) ? selected : [selected]
      if (arr.length === 0)
        return true
      const selectedValues = arr.map(v => Number(v))
      return selectedValues.includes(fight.Type)
    },
  },
])

const { selected, filtered } = useFilters(f.fights, filters.value)

// Apply text search on top of other filters
const searchFiltered = computed(() => {
  if (!searchText.value.trim())
    return filtered.value

  const search = searchText.value.toLowerCase()
  return filtered.value.filter(fight =>
    fight.Name.toLowerCase().includes(search)
    || fight.Description.toLowerCase().includes(search)
    || fight.FFLogsZoneName?.toLowerCase().includes(search)
    || fight.FFLogsExpansionName?.toLowerCase().includes(search),
  )
})
</script>

<template>
  <section class="home">
    <div class="page-header">
      <h2 class="page-title">
        Fights & Resources
      </h2>
    </div>

    <div class="search-section">
      <input
        v-model="searchText"
        class="search-input"
        placeholder="Search by name, description, zone, or expansion..."
        type="text"
      >
    </div>

    <FilterBar v-model="selected" :filters="filters" />

    <p class="results-count">
      {{ searchFiltered.length }} Fights are being shown
    </p>

    <CardList
      :columns="2"
      :items="searchFiltered"
      item-key="Id"
    >
      <template #item="{ item }">
        <FightCard :fight="item" :is-member="isMember?.valueOf()" />
      </template>
    </CardList>
  </section>
</template>

<style scoped>
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

.search-section {
  margin-bottom: 1rem;
}

.search-input {
  width: 100%;
  max-width: 600px;
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid var(--border);
  border-radius: 0.5rem;
  background: var(--card);
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

.results-count {
  margin: 1rem 0;
  color: var(--muted);
  font-size: 0.875rem;
}
</style>
