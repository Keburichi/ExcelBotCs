<script setup lang="ts">
import { ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'

const storageKey = 'theme'

type Theme = 'light' | 'dark'

function getSystemTheme(): Theme {
  if (typeof window === 'undefined')
    return 'light'
  return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function getCurrentTheme(): Theme {
  if (typeof document === 'undefined')
    return 'light'
  const ds = document.documentElement.dataset.theme as Theme | undefined
  return ds ?? getSystemTheme()
}

function applyTheme(next: Theme) {
  if (typeof document === 'undefined')
    return
  document.documentElement.setAttribute('data-theme', next)
}

function saveTheme(next: Theme) {
  try {
    localStorage.setItem(storageKey, next)
  }
  catch {
  }
}

const isDark = ref(getCurrentTheme() === 'dark')

function toggle() {
  const next: Theme = isDark.value ? 'light' : 'dark'
  applyTheme(next)
  saveTheme(next)
  isDark.value = !isDark.value
}
</script>

<template>
  <BaseButton
    :aria-pressed="isDark"
    :tooltip="isDark ? 'Switch to light theme' : 'Switch to dark theme'"
    icon-only
    rounded
    size="small"
    state="secondary"
    variant="outlined"
    @clicked="toggle"
  >
    <template #icon>
      <svg
        v-if="!isDark" aria-hidden="true" fill="currentColor" height="24" viewBox="0 0 24 24"
        width="24" xmlns="http://www.w3.org/2000/svg"
      >
        <path
          d="M6.76 4.84l-1.8-1.79-1.41 1.41 1.79 1.8 1.42-1.42zm10.45-1.79l-1.79 1.8 1.41 1.41 1.8-1.79-1.42-1.42zM12 4V1h-2v3h2zm0 19v-3h-2v3h2zM4 13H1v-2h3v2zm22 0h-3v-2h3v2zM6.76 19.16l-1.42 1.42-1.79-1.8 1.41-1.41 1.8 1.79zM20.45 17.37l-1.8 1.79-1.41-1.41 1.79-1.8 1.42 1.42zM12 6a6 6 0 000 12 6 6 0 000-12z"
        />
      </svg>
      <svg
        v-else aria-hidden="true" fill="currentColor" height="24" viewBox="0 0 24 24" width="24"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path d="M9.37 5.51A7 7 0 0012 19a7 7 0 006.49-9.63.5.5 0 00-.79-.17A5.5 5.5 0 0110 6.3a.5.5 0 00-.63-.79z" />
      </svg>
    </template>
  </BaseButton>
</template>

<style scoped>
</style>
