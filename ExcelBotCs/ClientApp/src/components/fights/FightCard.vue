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
  <BaseCard :title="props.fight.Name" variant="outlined" :clickable="true" @click="cardClick(props.fight)">
    <template #body>
      <p>{{ props.fight.Description }}</p>
    </template>
    <template #image>
      <img
        v-if="props.fight.ImageUrl" :src="props.fight.ImageUrl" :alt="props.fight.Name" class="card__image"
        referrerpolicy="no-referrer"
      >
      <div v-else class="card__image card__image--placeholder" :title="`No image available for ${props.fight.Name}`" />
    </template>
    <template #footer>
      <p>Difficulty: {{ fightTypeToString(props.fight.Type) }}</p>
    </template>
  </BaseCard>
</template>

<style scoped>
.card__image--placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  position: relative;
  overflow: hidden;
}

.card__image--placeholder::before {
  content: '';
  position: absolute;
  inset: 0;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='800' height='450' viewBox='0 0 800 450'%3E%3Crect width='800' height='450' fill='%23667eea' opacity='0.3'/%3E%3Cg fill='%23ffffff' opacity='0.4'%3E%3Ccircle cx='200' cy='150' r='80'/%3E%3Ccircle cx='600' cy='300' r='100'/%3E%3Ccircle cx='400' cy='200' r='60'/%3E%3C/g%3E%3C/svg%3E");
  background-size: cover;
  background-position: center;
}

.card__image--placeholder::after {
  content: '🎮';
  font-size: 4rem;
  position: relative;
  z-index: 1;
  opacity: 0.6;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.3));
}
</style>
