import type { Member } from '@/features/members/members.types'
import { computed, ref } from 'vue'

const authorized = ref(false)
const me = ref<Member | null>(null)

export function useAuth() {
  async function ensureAuth() {
    try {
      // e.g., ping an endpoint that returns 200 if logged in
      const res = await fetch('/api/auth', { method: 'HEAD', credentials: 'same-origin' })
      authorized.value = res.ok
    }
    catch {
      authorized.value = false
    }
    return authorized.value
  }

  function login(redirect?: string) {
    const url = `/api/discord/login${redirect ? `?redirect=${encodeURIComponent(redirect)}` : ''}`
    window.location.href = url
  }

  async function logout() {
    await fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' })
    me.value = null
    authorized.value = false
    window.location.href = '/login'
  }

  async function loadMe() {
    // We don't have to query the server if we already have the user info
    if (me.value)
      return

    try {
      const res = await fetch('/api/auth/me', { credentials: 'same-origin' })

      switch (res.status) {
        case 200:
          me.value = await res.json()
          authorized.value = true
          break
        case 401: // not logged in
          authorized.value = false
          me.value = null
          break
        default:
          throw new Error('Failed to load user info')
      }
    }
    catch {
      authorized.value = false
      me.value = null
    }
  }

  const user = computed(() => me.value)
  const isAdmin = computed(() => me.value?.IsAdmin)
  const isMember = computed(() => me.value?.IsMember)

  return { authorized, user, isAdmin, isMember, loadMe, ensureAuth, login, logout }
}
