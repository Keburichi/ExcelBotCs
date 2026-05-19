<script setup lang="ts">
import type { Fight } from '@/features/fights/fights.types'
import { computed } from 'vue'
import { FightType } from '@/features/fights/fights.types'

const props = defineProps<{
  experience: Fight[]
}>()

// Map Ultimate fight names to their abbreviations
const ultimateAbbreviations: Record<string, string> = {
  'The Unending Coil of Bahamut': 'UCOB',
  'Unending Coil of Bahamut': 'UCOB',
  'Bahamut Prime': 'UCOB',
  'The Weapon\'s Refrain': 'UWU',
  'Weapon\'s Refrain': 'UWU',
  'Ultima Weapon': 'UWU',
  'The Epic of Alexander': 'TEA',
  'Epic of Alexander': 'TEA',
  'Dragonsong\'s Reprise': 'DSR',
  'Dragonsong Reprise': 'DSR',
  'The Omega Protocol': 'TOP',
  'Omega Protocol': 'TOP',
  'Futures Rewritten': 'FRU',
}

function getUltimateAbbreviation(fightName: string): string {
  // Check exact matches first
  if (ultimateAbbreviations[fightName]) {
    return ultimateAbbreviations[fightName]
  }

  // Check partial matches
  for (const [key, value] of Object.entries(ultimateAbbreviations)) {
    if (fightName.includes(key) || key.includes(fightName)) {
      return value
    }
  }

  // Fallback to first letters
  return fightName.split(' ').map(word => word[0]).join('').toUpperCase().substring(0, 4)
}

const experienceSummary = computed(() => {
  const hasExtreme = props.experience.some(f => f.Type === FightType.Extreme)
  const hasSavage = props.experience.some(f => f.Type === FightType.Savage)
  const hasChaotic = props.experience.some(f => f.Type === FightType.Chaotic)

  const ultimates = Array.from(
    new Set(
      props.experience
        .filter(f => f.Type === FightType.Ultimate)
        .map(f => getUltimateAbbreviation(f.Name)),
    ),
  )

  return {
    hasExtreme,
    hasSavage,
    hasChaotic,
    ultimates,
  }
})
</script>

<template>
  <div class="experience-tags">
    <span v-if="experienceSummary.hasExtreme" class="tag tag--extreme" title="Has Extreme experience">
      Extreme
    </span>
    <span v-if="experienceSummary.hasSavage" class="tag tag--savage" title="Has Savage experience">
      Savage
    </span>
    <span v-if="experienceSummary.hasChaotic" class="tag tag--chaotic" title="Has Chaotic experience">
      Chaotic
    </span>
    <span
      v-for="ultimate in experienceSummary.ultimates"
      :key="ultimate"
      class="tag tag--ultimate"
      :title="`Cleared ${ultimate}`"
    >
      {{ ultimate }}
    </span>
  </div>
</template>

<style scoped>
.experience-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  align-items: center;
}

.tag {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  border-radius: 8px;
  font-size: 0.75rem;
  font-weight: 600;
  line-height: 1;
  white-space: nowrap;
  border: 1px solid transparent;
}

.tag--extreme {
  background: var(--exp-extreme);
  color: var(--bg);
  border-color: var(--exp-extreme-border);
}

.tag--savage {
  background: var(--exp-savage);
  color: var(--bg);
  border-color: var(--exp-savage-border);
}

.tag--chaotic {
  background: var(--exp-chaotic);
  color: var(--bg);
  border-color: var(--exp-chaotic-border);
}

.tag--ultimate {
  background: linear-gradient(135deg, var(--exp-ultimate-from) 0%, var(--exp-ultimate-to) 100%);
  color: var(--bg);
  border-color: var(--exp-ultimate-border);
  font-weight: 700;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}
</style>
