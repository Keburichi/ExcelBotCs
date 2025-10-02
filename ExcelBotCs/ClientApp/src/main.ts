import { createPinia } from 'pinia'
import { createApp } from 'vue'
import AppShell from '@/app/AppShell.vue'
import { router } from '@/app/router'
import '@/styles/main.css'
import '@/styles/cards.css'

// Initialize theme early to avoid FOUC
(function initTheme() {
  try {
    const key = 'theme'
    let theme = localStorage.getItem(key) as 'light' | 'dark' | null
    if (theme !== 'light' && theme !== 'dark') {
      const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
      theme = prefersDark ? 'dark' : 'light'
    }
    document.documentElement.setAttribute('data-theme', theme!)
  }
  catch {
    // ignore
  }
})()

const pinia = createPinia()
const app = createApp(AppShell)

app.use(router)
app.use(pinia)
app.mount('#app')
