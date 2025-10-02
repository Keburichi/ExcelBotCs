<script setup lang="ts">
import type { Fight } from '@/features/fights/fights.types'
import BaseCard from '@/components/BaseCard.vue'
import { fightTypeToString } from '@/features/fights/fights.types'

const props = defineProps<{
  fight: Fight
  isMember?: boolean
}>()

function cardClick(fight: Fight) {
  alert(fight.Name)
}
</script>

<template>
  <BaseCard :title="props.fight.Name" variant="elevated" :clickable="true" @click="cardClick(props.fight)">
    <template #body>
      <p>{{ props.fight.Description }}</p>
    </template>
    <template #image>
      <img
        v-if="props.fight.ImageUrl" :src="props.fight.ImageUrl" :alt="props.fight.Name" class="card__image"
        referrerpolicy="no-referrer"
      >
      <span v-else class="card__image placeholder">?</span>
    </template>
    <template #footer>
      <p>Difficulty: {{ fightTypeToString(props.fight.Type) }}</p>
    </template>
  </BaseCard>
</template>

<style scoped>

</style>
