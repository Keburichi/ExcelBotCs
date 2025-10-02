<script setup lang="ts">
import type { Fight } from '@/features/fights/fights.types'
import type { FilterDef } from '@/utils/filters.types'
import { onMounted } from 'vue'
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

const fightTypeOptions = [
  { label: 'Extreme', value: FightType.Extreme.valueOf() },
  { label: 'Savage', value: FightType.Savage },
  { label: 'Ultimate', value: FightType.Ultimate },
]

const filters: FilterDef<Fight>[] = [
  {
    id: 'type',
    label: 'Fight Type',
    multiple: true,
    options: fightTypeOptions,
    predicate: (fight, selected) => {
      const arr = Array.isArray(selected) ? selected : [selected]

      if (arr.length === 0)
        return true

      // convert each selected value to a number (needed since the select component returns strings)
      const selectedValues = arr.map(v => Number(v))
      return selectedValues.includes(fight.Type)
    },
  },
]

const { selected, filtered } = useFilters(f.fights, filters)
</script>

<template>
  <section class="home">
    <h2>Fights</h2>

    <FilterBar v-model="selected" :filters="filters" />

    <p>{{ filtered.length }} Fights are being shown</p>

    <CardList
      :items="filtered"
      :columns="3"
      item-key="Id"
    >
      <template #item="{ item }">
        <FightCard :fight="item" :is-member="isMember?.valueOf()" />
      </template>
    </CardList>
  </section>
</template>

<style scoped>

</style>
