<script lang="ts" setup>
import { computed, ref, watch } from 'vue'
import type { BonusLotteryDrawResponse } from '@/features/lottery/lottery.types'
import BaseModal from '@/components/BaseModal.vue'
import SpinningWheel from './SpinningWheel.vue'
import type { WheelSegment } from './SpinningWheel.vue'

const props = defineProps<{
  drawResult: BonusLotteryDrawResponse | null
}>()

const isOpen = defineModel<boolean>({ required: true })

type Phase = 'idle' | 'phase1-spinning' | 'phase1-result' | 'phase2-spinning' | 'phase2-result' | 'no-winner'
const phase = ref<Phase>('idle')

const phase1Spinning = ref(false)
const phase2Spinning = ref(false)

const WHEEL_WIN_COLOR = 'oklch(0.72 0.20 145)'
const WHEEL_DUD_COLOR = 'oklch(0.55 0.12 25)'

const phase1Segments = computed<WheelSegment[]>(() => [
  { label: 'WIN', color: WHEEL_WIN_COLOR },
  { label: 'TRY AGAIN', color: WHEEL_DUD_COLOR },
  { label: 'TRY AGAIN', color: 'oklch(0.50 0.10 30)' },
  { label: 'TRY AGAIN', color: WHEEL_DUD_COLOR },
  { label: 'TRY AGAIN', color: 'oklch(0.50 0.10 30)' },
])

const phase1Target = computed(() => {
  if (!props.drawResult) return 1
  return props.drawResult.HasWinner ? 0 : 1 + Math.floor(Math.random() * 4)
})

const segmentColors = [
  'oklch(0.65 0.15 250)',
  'oklch(0.65 0.15 295)',
  'oklch(0.65 0.15 165)',
  'oklch(0.65 0.15 70)',
  'oklch(0.65 0.15 330)',
  'oklch(0.60 0.15 210)',
  'oklch(0.60 0.15 120)',
  'oklch(0.60 0.15 45)',
]

const phase2Segments = computed<WheelSegment[]>(() => {
  if (!props.drawResult) return []
  return props.drawResult.AllEntries.map((entry, i) => ({
    label: entry.DiscordName,
    color: segmentColors[i % segmentColors.length],
  }))
})

const phase2Target = computed(() => {
  if (!props.drawResult) return 0
  return props.drawResult.WinnerIndex
})

function startSpin() {
  phase.value = 'phase1-spinning'
  phase1Spinning.value = true
}

function onPhase1Complete() {
  if (!props.drawResult) return

  if (props.drawResult.HasWinner) {
    phase.value = 'phase1-result'
    setTimeout(() => {
      phase.value = 'phase2-spinning'
      phase2Spinning.value = true
    }, 1500)
  }
  else {
    phase.value = 'no-winner'
  }
}

function onPhase2Complete() {
  phase.value = 'phase2-result'
}

function closeGuard(): boolean {
  if (phase.value === 'phase1-spinning' || phase.value === 'phase2-spinning') {
    return false
  }
  return true
}

watch(isOpen, (open) => {
  if (open) {
    phase.value = 'idle'
    phase1Spinning.value = false
    phase2Spinning.value = false
  }
})
</script>

<template>
  <BaseModal
    v-model="isOpen"
    title="Bonus Lottery"
    size="medium"
    :close-on-outside-click="false"
    :close-guard="closeGuard"
  >
    <template #body>
      <div class="bonus-lottery-content">
        <!-- Prize display -->
        <div v-if="drawResult" class="prize-banner">
          Prize: <strong>{{ drawResult.Prize }}</strong>
        </div>

        <!-- Phase 1: Win/Dud wheel -->
        <div v-if="phase === 'idle' || phase === 'phase1-spinning' || phase === 'phase1-result'" class="wheel-phase">
          <p class="phase-label">
            {{ phase === 'idle' ? 'Will someone win today?' : phase === 'phase1-result' ? 'We have a winner!' : 'Spinning...' }}
          </p>
          <SpinningWheel
            :segments="phase1Segments"
            :target-index="phase1Target"
            :spinning="phase1Spinning"
            :duration="4000"
            @spin-complete="onPhase1Complete"
          />
        </div>

        <!-- No winner result -->
        <div v-if="phase === 'no-winner'" class="result-display result-display--dud">
          <div class="result-icon">
            &#x2715;
          </div>
          <h3>No winner this time!</h3>
          <p>Better luck on the next bonus draw.</p>
        </div>

        <!-- Phase 2: Winner wheel -->
        <div v-if="phase === 'phase2-spinning' || phase === 'phase2-result'" class="wheel-phase">
          <p class="phase-label">
            {{ phase === 'phase2-spinning' ? 'Who will it be?' : '' }}
          </p>
          <SpinningWheel
            :segments="phase2Segments"
            :target-index="phase2Target"
            :spinning="phase2Spinning"
            :duration="5000"
            @spin-complete="onPhase2Complete"
          />
        </div>

        <!-- Winner result -->
        <div v-if="phase === 'phase2-result' && drawResult?.Winner" class="result-display result-display--win">
          <div class="result-icon">
            &#x2728;
          </div>
          <h3>Congratulations!</h3>
          <p class="winner-name">
            {{ drawResult.Winner.DiscordName }}
          </p>
          <p class="winner-prize">
            wins <strong>{{ drawResult.Prize }}</strong>
          </p>
        </div>

        <!-- Idle state: spin button -->
        <div v-if="phase === 'idle'" class="spin-action">
          <button class="spin-button" type="button" @click="startSpin">
            Spin the Wheel!
          </button>
          <p class="entry-count">
            {{ drawResult?.AllEntries.length ?? 0 }} entries in the pool
          </p>
        </div>
      </div>
    </template>
  </BaseModal>
</template>

<style scoped>
.bonus-lottery-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
  padding: 1rem 0;
}

.prize-banner {
  background: color-mix(in oklab, var(--link) 10%, transparent);
  border: 1px solid color-mix(in oklab, var(--link) 30%, transparent);
  border-radius: 12px;
  padding: 0.625rem 1.25rem;
  font-size: 0.95rem;
  color: var(--fg);
}

.wheel-phase {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.phase-label {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--fg);
  margin: 0;
  min-height: 1.5rem;
}

.result-display {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2rem;
  border-radius: 16px;
  animation: fadeIn 0.4s ease;
}

.result-display--dud {
  background: color-mix(in oklab, var(--danger) 8%, transparent);
}

.result-display--win {
  background: color-mix(in oklab, oklch(0.72 0.20 145) 10%, transparent);
}

.result-icon {
  font-size: 2.5rem;
  line-height: 1;
}

.result-display h3 {
  margin: 0;
  font-size: 1.3rem;
  color: var(--fg);
}

.result-display p {
  margin: 0;
  color: var(--muted);
}

.winner-name {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--fg) !important;
}

.winner-prize {
  font-size: 1rem;
}

.spin-action {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.spin-button {
  padding: 0.875rem 2.5rem;
  font-size: 1.1rem;
  font-weight: 700;
  color: #fff;
  background: var(--link);
  border: none;
  border-radius: 12px;
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.spin-button:hover {
  transform: scale(1.03);
  box-shadow: 0 4px 16px color-mix(in oklab, var(--link) 40%, transparent);
}

.spin-button:active {
  transform: scale(0.98);
}

.entry-count {
  margin: 0;
  font-size: 0.8rem;
  color: var(--muted);
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: scale(0.9);
  }
  to {
    opacity: 1;
    transform: scale(1);
  }
}

@media (prefers-reduced-motion: reduce) {
  .result-display {
    animation: none;
  }
}
</style>
