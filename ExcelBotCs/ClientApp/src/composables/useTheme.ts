import { onMounted, ref, watch } from 'vue'

type Theme = 'light' | 'dark'
const storageKey = 'theme'

const isDark = ref<boolean>(false)

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

function applyTheme(theme: Theme) {
  if (typeof document === 'undefined')
    return
  document.documentElement.setAttribute('data-theme', theme)
  isDark.value = theme === 'dark'
}

function saveTheme(theme: Theme) {
  try {
    localStorage.setItem(storageKey, theme)
  }
  catch {
    // ignore
  }
}

function toggleTheme() {
  const next: Theme = isDark.value ? 'light' : 'dark'
  applyTheme(next)
  saveTheme(next)
}

function initializeTheme() {
  const savedTheme = localStorage.getItem(storageKey) as Theme | null
  const theme = savedTheme ?? getSystemTheme()
  applyTheme(theme)
}

export function useTheme() {
  onMounted(initializeTheme)

  watch(
    () => isDark.value,
    (newIsDark) => {
      applyTheme(newIsDark ? 'dark' : 'light')
    },
  )

  return {
    isDark,
    toggleTheme,
  }
}
