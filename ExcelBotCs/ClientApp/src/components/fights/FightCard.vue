<script lang="ts" setup>
import type { Fight } from '@/features/fights/fights.types'
import { ref } from 'vue'
import BaseCard from '@/components/BaseCard.vue'
import RaidplanDialog from '@/components/fights/RaidplanDialog.vue'
import { fightTypeToString } from '@/features/fights/fights.types'

const props = defineProps<{
  fight: Fight
  isMember?: boolean
}>()

const showRaidplanDialog = ref(false)

function cardClick(fight: Fight) {
  showRaidplanDialog.value = true
}
</script>

<template>
  <BaseCard :clickable="true" :title="props.fight.Name" variant="outlined" @click="cardClick(props.fight)">
    <template #body>
      <div class="fight-info">
        <p class="fight-description">
          {{ props.fight.Description }}
        </p>
      </div>
    </template>
    <template #image>
      <img
        v-if="props.fight.ImageUrl" :alt="props.fight.Name" :src="props.fight.ImageUrl" class="card__image"
        referrerpolicy="no-referrer"
      >
      <div v-else :title="`No image available for ${props.fight.Name}`" class="card__image card__image--placeholder" />
    </template>
    <template #footer>
      <div class="fight-metadata">
        <span
          :class="`difficulty-${fightTypeToString(props.fight.Type).toLowerCase()}`"
          class="fight-badge difficulty-badge"
        >
          {{ fightTypeToString(props.fight.Type) }}
        </span>
        <span v-if="props.fight.FFLogsExpansionName" class="fight-badge expansion-badge">
          {{ props.fight.FFLogsExpansionName }}
        </span>
        <span v-if="props.fight.FFLogsZoneName" class="fight-badge zone-badge">
          {{ props.fight.FFLogsZoneName }}
        </span>
      </div>
    </template>
  </BaseCard>

  <!-- Raidplan Dialog -->
  <RaidplanDialog
    v-model:is-open="showRaidplanDialog"
    :fight="props.fight"
    @close="showRaidplanDialog = false"
  />
</template>

<style scoped>
/* Override BaseCard title styling for fights */
.card :deep(.card__title) {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--fg);
  margin-bottom: 0.75rem;
  line-height: 1.3;
}

.card :deep(.card__header) {
  padding-bottom: 0.5rem;
  border-bottom: 2px solid var(--border);
  margin-bottom: 1rem;
}

.fight-info {
  display: flex;
  flex-direction: column;
}

.fight-description {
  color: var(--muted);
  line-height: 1.6;
  font-size: 0.95rem;
  font-weight: 400;
}

.fight-metadata {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.fight-badge {
  display: inline-block;
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  white-space: nowrap;
}

/* Difficulty badges */
.difficulty-badge.difficulty-normal {
  background: var(--cat-blue-bg);
  color: var(--cat-blue-fg);
}

.difficulty-badge.difficulty-extreme {
  background: var(--cat-purple-bg);
  color: var(--cat-purple-fg);
}

.difficulty-badge.difficulty-savage {
  background: var(--cat-red-bg);
  color: var(--cat-red-fg);
}

.difficulty-badge.difficulty-legacysavage {
  background: var(--cat-rose-bg);
  color: var(--cat-rose-fg);
}

.difficulty-badge.difficulty-ultimate {
  background: var(--cat-orange-bg);
  color: var(--cat-orange-fg);
}

.difficulty-badge.difficulty-chaotic {
  background: var(--cat-indigo-bg);
  color: var(--cat-indigo-fg);
}

/* Expansion badge */
.expansion-badge {
  background: var(--cat-green-bg);
  color: var(--cat-green-fg);
}

/* Zone badge */
.zone-badge {
  background: var(--cat-teal-bg);
  color: var(--cat-teal-fg);
}

.card__image {
  /* zoom in on the image since the fight images have a small white gradient */
  transform: scale(1.1);
}

.card__image--placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--exp-ultimate-from) 0%, var(--exp-ultimate-to) 100%);
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
