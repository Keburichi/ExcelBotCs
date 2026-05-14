<script setup lang="ts">
import type { Fight } from '@/features/fights/fights.types'
import type { FilterDef } from '@/utils/filters.types'
import { computed, onMounted, ref } from 'vue'
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
  <section>
    <div class="fights-toolbar">
      <input
        v-model="searchText"
        class="fights-search"
        placeholder="Search fights..."
        type="text"
      >
      <FilterBar v-model="selected" :filters="filters" />
      <span class="fights-count">{{ searchFiltered.length }} fights</span>
    </div>

    <div v-if="f.loading.value" class="fights-empty muted">
      Loading...
    </div>

    <div v-else-if="f.error.value" class="fights-empty error">
      {{ f.error.value }}
    </div>

    <div v-else-if="searchFiltered.length === 0" class="fights-empty muted">
      No fights found.
    </div>

    <div v-else class="fights-grid">
      <FightCard
        v-for="fight in searchFiltered"
        :key="fight.Id"
        :fight="fight"
        :is-member="isMember?.valueOf()"
      />
    </div>
  </section>
</template>

<style scoped>
.fights-toolbar {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
  align-items: center;
  margin-bottom: 1.5rem;
}

.fights-search {
  flex: 0 1 320px;
}

.fights-count {
  color: var(--muted);
  font-size: 0.875rem;
  margin-left: auto;
  white-space: nowrap;
}

.fights-empty {
  text-align: center;
  padding: 3rem 1rem;
}

.fights-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

@media (max-width: 640px) {
  .fights-toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  .fights-search {
    flex: 1;
  }

  .fights-count {
    margin-left: 0;
  }

  .fights-grid {
    grid-template-columns: 1fr;
  }
}
</style>
