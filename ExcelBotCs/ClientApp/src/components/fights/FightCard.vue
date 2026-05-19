<script lang="ts" setup>
import type { Fight } from '@/features/fights/fights.types'
import { ref } from 'vue'
import RaidplanDialog from '@/components/fights/RaidplanDialog.vue'
import { fightTypeToString } from '@/features/fights/fights.types'

const props = defineProps<{
  fight: Fight
  isMember?: boolean
}>()

const showRaidplanDialog = ref(false)

function cardClick() {
  showRaidplanDialog.value = true
}

function difficultyClass(fight: Fight) {
  return `difficulty-${fightTypeToString(fight.Type).toLowerCase()}`
}
</script>

<template>
  <article
    class="fight-card"
    tabindex="0"
    role="button"
    @click="cardClick"
    @keydown.enter.space.prevent="cardClick"
  >
    <div class="fight-card__image-wrap">
      <img
        v-if="fight.ImageUrl"
        :alt="fight.Name"
        :src="fight.ImageUrl"
        class="fight-card__image"
        referrerpolicy="no-referrer"
        loading="lazy"
      >
      <div v-else class="fight-card__image fight-card__placeholder" :class="difficultyClass(fight)">
        <svg class="fight-card__placeholder-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M14.5 3.5C14.5 3.5 14.5 5.5 12 5.5C9.5 5.5 9.5 3.5 9.5 3.5" />
          <path d="M4.5 8.5L8 10.5V14L4.5 17.5" />
          <path d="M19.5 8.5L16 10.5V14L19.5 17.5" />
          <path d="M8 10.5L12 8L16 10.5" />
          <path d="M12 8V3.5" />
          <path d="M8 14H16" />
          <path d="M10 14V20.5H14V14" />
        </svg>
      </div>

      <div class="fight-card__image-overlay">
        <span v-if="fight.Resources?.length" class="fight-card__resource-count">
          {{ fight.Resources.length }} {{ fight.Resources.length === 1 ? 'resource' : 'resources' }}
        </span>
      </div>
    </div>

    <div class="fight-card__body">
      <h3 class="fight-card__name">
        {{ fight.Name }}
      </h3>
      <div class="fight-card__meta">
        <span class="fight-card__badge" :class="difficultyClass(fight)">
          {{ fightTypeToString(fight.Type) }}
        </span>
        <span v-if="fight.FFLogsExpansionName" class="fight-card__badge fight-card__badge--expansion">
          {{ fight.FFLogsExpansionName }}
        </span>
      </div>
    </div>
  </article>

  <RaidplanDialog
    v-model:is-open="showRaidplanDialog"
    :fight="fight"
    @close="showRaidplanDialog = false"
  />
</template>

<style scoped>
.fight-card {
  position: relative;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.3);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
  overflow: hidden;
  cursor: pointer;
  transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;
  display: flex;
  flex-direction: column;
}

:root[data-theme='dark'] .fight-card {
  background: rgba(18, 26, 45, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .fight-card {
    background: rgba(18, 26, 45, 0.7);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.fight-card:hover {
  transform: translateY(-2px);
  border-color: rgba(59, 130, 246, 0.3);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.15),
    0 4px 16px rgba(0, 0, 0, 0.1),
    inset 0 1px 0 rgba(255, 255, 255, 0.6);
}

:root[data-theme='dark'] .fight-card:hover {
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .fight-card:hover {
    border-color: rgba(59, 130, 246, 0.4);
    box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
      0 4px 16px rgba(0, 0, 0, 0.4),
      inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
}

.fight-card:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

/* Image area */
.fight-card__image-wrap {
  position: relative;
  overflow: hidden;
  aspect-ratio: 16 / 9;
}

.fight-card__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transform: scale(1.05);
  transition: transform 0.3s ease;
}

.fight-card:hover .fight-card__image {
  transform: scale(1.1);
}

/* Placeholder */
.fight-card__placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
}

.fight-card__placeholder.difficulty-extreme {
  background: linear-gradient(135deg, oklch(0.40 0.10 165), oklch(0.30 0.08 185));
}

.fight-card__placeholder.difficulty-savage {
  background: linear-gradient(135deg, oklch(0.38 0.12 25), oklch(0.28 0.10 10));
}

.fight-card__placeholder.difficulty-legacysavage {
  background: linear-gradient(135deg, oklch(0.38 0.12 295), oklch(0.28 0.10 310));
}

.fight-card__placeholder.difficulty-ultimate {
  background: linear-gradient(135deg, oklch(0.38 0.12 265), oklch(0.30 0.10 300));
}

.fight-card__placeholder.difficulty-chaotic {
  background: linear-gradient(135deg, oklch(0.40 0.10 280), oklch(0.30 0.08 260));
}

.fight-card__placeholder.difficulty-normal {
  background: linear-gradient(135deg, oklch(0.42 0.08 250), oklch(0.32 0.06 230));
}

.fight-card__placeholder.difficulty-unreal {
  background: linear-gradient(135deg, oklch(0.40 0.10 70), oklch(0.30 0.08 50));
}

.fight-card__placeholder-icon {
  width: 48px;
  height: 48px;
  color: rgba(255, 255, 255, 0.3);
}

/* Hover overlay with resource count */
.fight-card__image-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.5) 0%, transparent 50%);
  opacity: 0;
  transition: opacity 0.2s ease;
  display: flex;
  align-items: flex-end;
  justify-content: flex-end;
  padding: 0.75rem;
}

.fight-card:hover .fight-card__image-overlay {
  opacity: 1;
}

.fight-card__resource-count {
  font-size: 0.8rem;
  font-weight: 500;
  color: rgba(255, 255, 255, 0.95);
  background: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(8px);
  padding: 0.25rem 0.625rem;
  border-radius: 999px;
}

/* Body */
.fight-card__body {
  padding: 0.875rem 1rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.fight-card__name {
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.fight-card__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
}

.fight-card__badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 999px;
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  white-space: nowrap;
}

.fight-card__badge.difficulty-normal {
  background: var(--cat-blue-bg);
  color: var(--cat-blue-fg);
}

.fight-card__badge.difficulty-extreme {
  background: var(--cat-purple-bg);
  color: var(--cat-purple-fg);
}

.fight-card__badge.difficulty-savage {
  background: var(--cat-red-bg);
  color: var(--cat-red-fg);
}

.fight-card__badge.difficulty-legacysavage {
  background: var(--cat-rose-bg);
  color: var(--cat-rose-fg);
}

.fight-card__badge.difficulty-ultimate {
  background: var(--cat-orange-bg);
  color: var(--cat-orange-fg);
}

.fight-card__badge.difficulty-chaotic {
  background: var(--cat-indigo-bg);
  color: var(--cat-indigo-fg);
}

.fight-card__badge.difficulty-unreal {
  background: var(--cat-amber-bg);
  color: var(--cat-amber-fg);
}

.fight-card__badge--expansion {
  background: var(--cat-green-bg);
  color: var(--cat-green-fg);
}

@media (prefers-reduced-motion: reduce) {
  .fight-card,
  .fight-card__image,
  .fight-card__image-overlay {
    transition: none !important;
  }
}
</style>
