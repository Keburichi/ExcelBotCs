<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { LotteryApi } from '@/features/lottery/lottery.api'

// The member reminder opt-in (spec §5.7, #112). Default off. The PUT returns the stored state, so the switch
// shows exactly what the server kept; a failed save rolls the optimistic flip back and says so.
const enabled = ref(false)
const loading = ref(true)
const saving = ref(false)
const error = ref('')

onMounted(async () => {
  try {
    const state = await LotteryApi.getReminders()
    enabled.value = state.Enabled
  }
  catch {
    error.value = 'Could not load your reminder setting.'
  }
  finally {
    loading.value = false
  }
})

async function toggle() {
  if (loading.value || saving.value)
    return

  const previous = enabled.value
  enabled.value = !previous
  saving.value = true
  error.value = ''

  try {
    const state = await LotteryApi.setReminders(enabled.value)
    enabled.value = state.Enabled
  }
  catch {
    enabled.value = previous
    error.value = 'Could not save your reminder setting. Please try again.'
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="reminder-toggle">
    <div class="reminder-toggle__copy">
      <span id="lottery-reminder-title" class="reminder-toggle__title">Weekly reminder</span>
      <span id="lottery-reminder-hint" class="reminder-toggle__hint">
        Get a Discord reminder before the draw when you still have guesses left.
      </span>
    </div>

    <button
      type="button"
      role="switch"
      class="switch"
      :class="{ 'switch--on': enabled }"
      :aria-checked="enabled"
      :disabled="loading || saving"
      aria-labelledby="lottery-reminder-title"
      aria-describedby="lottery-reminder-hint"
      @click="toggle"
    >
      <span class="switch__thumb" />
    </button>

    <p v-if="error" class="reminder-toggle__error" role="alert">
      {{ error }}
    </p>
  </div>
</template>

<style scoped>
.reminder-toggle {
  display: flex;
  align-items: center;
  gap: 0.875rem;
  padding: 0.875rem 1.25rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--card);
}

.reminder-toggle__copy {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
}

.reminder-toggle__title {
  color: var(--fg);
  font-size: 0.85rem;
  font-weight: 600;
}

.reminder-toggle__hint {
  color: var(--muted);
  font-size: 0.75rem;
  line-height: 1.45;
}

.reminder-toggle__error {
  flex-basis: 100%;
  margin: 0;
  color: var(--msg-error-fg);
  font-size: 0.75rem;
}

.switch {
  position: relative;
  flex-shrink: 0;
  margin-left: auto;
  width: 2.5rem;
  height: 1.4rem;
  padding: 0;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: color-mix(in oklab, var(--fg) 12%, var(--card));
  transition: background 200ms ease, border-color 200ms ease;
}

.switch:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.switch:disabled {
  cursor: progress;
  opacity: 0.65;
}

.switch--on {
  border-color: color-mix(in oklab, var(--link) 60%, var(--border));
  background: var(--link);
}

.switch__thumb {
  position: absolute;
  top: 50%;
  left: 0.15rem;
  width: 1.02rem;
  height: 1.02rem;
  border-radius: 50%;
  background: #fff;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
  transform: translateY(-50%);
  transition: left 200ms ease;
}

.switch--on .switch__thumb {
  left: calc(100% - 1.17rem);
}
</style>
