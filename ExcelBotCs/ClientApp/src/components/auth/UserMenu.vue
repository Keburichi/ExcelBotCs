<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '@/composables/useAuth'

const { authorized, user, logout, loadMe } = useAuth()
const open = ref(false)
const router = useRouter()
const menuRef = ref<HTMLElement | null>(null)

onMounted(() => {
  void loadMe()
  document.addEventListener('click', onClickOutside)
  document.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('keydown', onKeydown)
})

function toggle() {
  open.value = !open.value
  if (open.value) {
    nextTick(() => {
      const first = menuRef.value?.querySelector<HTMLElement>('[role="menuitem"]')
      first?.focus()
    })
  }
}

function goProfile() {
  open.value = false
  router.push('/profile')
}

function handleLogout() {
  open.value = false
  logout()
}

function onClickOutside(e: MouseEvent) {
  const target = e.target as HTMLElement | null
  if (!target) return
  if (!(target.closest && target.closest('.user-menu'))) {
    open.value = false
  }
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && open.value) {
    open.value = false
  }
}

function initial(name: string | null | undefined): string {
  if (!name) return '?'
  return name.charAt(0).toUpperCase()
}
</script>

<template>
  <div v-if="authorized" class="user-menu">
    <button
      class="avatar-btn"
      aria-haspopup="menu"
      :aria-expanded="open"
      @click.stop="toggle"
    >
      <img
        v-if="user?.DiscordAvatar"
        :src="user!.DiscordAvatar"
        alt="Profile"
        class="avatar"
        referrerpolicy="no-referrer"
      >
      <span v-else class="avatar placeholder">{{
        initial(user?.PlayerName ?? user?.DiscordName)
      }}</span>
    </button>

    <div v-if="open" ref="menuRef" class="menu" role="menu">
      <div class="menu-header">
        <strong>{{ user?.PlayerName ?? user?.DiscordName }}</strong>
      </div>
      <button role="menuitem" class="menu-item" @click="goProfile">
        Profile
      </button>
      <div class="menu-divider" />
      <button role="menuitem" class="menu-item menu-item--danger" @click="handleLogout">
        Logout
      </button>
    </div>
  </div>

  <RouterLink v-else to="/login" class="login-link">
    Login
  </RouterLink>
</template>

<style scoped>
.user-menu {
  position: relative;
  z-index: 1001;
}

.avatar-btn {
  background: transparent;
  border: 0;
  padding: 2px;
  cursor: pointer;
  border-radius: 9999px;
  transition: box-shadow 0.2s ease;
}

.avatar-btn:hover {
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.4);
}

.avatar-btn:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.avatar {
  width: 36px;
  height: 36px;
  border-radius: 9999px;
  display: block;
  object-fit: cover;
}

.avatar.placeholder {
  width: 36px;
  height: 36px;
  border-radius: 9999px;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  color: #fff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 0.875rem;
  line-height: 1;
}

.menu {
  position: absolute;
  right: 0;
  top: calc(100% + 8px);
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(20px);
  color: var(--fg);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 12px;
  min-width: 200px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
  padding: 0.375rem;
  z-index: 1002;
  animation: menuAppear 0.2s ease-out;
}

:root[data-theme='dark'] .menu {
  background: rgba(18, 26, 45, 0.9);
  border-color: rgba(255, 255, 255, 0.15);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .menu {
    background: rgba(18, 26, 45, 0.9);
    border-color: rgba(255, 255, 255, 0.15);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4),
      inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

@keyframes menuAppear {
  from {
    opacity: 0;
    transform: translateY(-6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.menu-header {
  padding: 0.5rem 0.625rem;
  margin-bottom: 0.25rem;
  font-size: 0.875rem;
}

.menu-header strong {
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.menu-divider {
  height: 1px;
  background: var(--border);
  margin: 0.25rem 0.625rem;
  opacity: 0.6;
}

.menu-item {
  display: block;
  width: 100%;
  padding: 0.5rem 0.625rem;
  border: none;
  background: transparent;
  color: var(--fg);
  font-size: 0.875rem;
  font-weight: 500;
  text-align: left;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.15s ease;
}

.menu-item:hover {
  background: rgba(59, 130, 246, 0.08);
}

.menu-item:focus-visible {
  outline: none;
  box-shadow: 0 0 0 2px var(--ring);
}

:root[data-theme='dark'] .menu-item:hover {
  background: rgba(59, 130, 246, 0.15);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .menu-item:hover {
    background: rgba(59, 130, 246, 0.15);
  }
}

.menu-item--danger {
  color: var(--danger);
}

.menu-item--danger:hover {
  background: rgba(220, 38, 38, 0.08);
}

:root[data-theme='dark'] .menu-item--danger:hover {
  background: rgba(248, 113, 113, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .menu-item--danger:hover {
    background: rgba(248, 113, 113, 0.12);
  }
}

.login-link {
  display: inline-flex;
  align-items: center;
  padding: 0.375rem 1rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--link);
  border: 2px solid var(--link);
  border-radius: 8px;
  text-decoration: none;
  transition: all 0.2s ease;
}

.login-link:hover {
  background: rgba(59, 130, 246, 0.06);
  text-decoration: none;
}

.login-link:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

@media (prefers-reduced-motion: reduce) {
  .menu,
  .avatar-btn {
    animation: none !important;
    transition: none !important;
  }
}
</style>
