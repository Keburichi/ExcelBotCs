<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import { useAuth } from '@/composables/useAuth'

const { authorized, user, ensureAuth, logout, loadMe } = useAuth()
const open = ref(false)
const router = useRouter()

onMounted(() => {
  void loadMe()
})

function toggle() {
  open.value = !open.value
}

function goProfile() {
  open.value = false
  router.push('/profile')
}

function onClickOutside(e: MouseEvent) {
  const target = e.target as HTMLElement | null
  if (!target)
    return
  // close if click is outside the menu root
  if (!(target.closest && target.closest('.user-menu'))) {
    open.value = false
  }
}

if (typeof window !== 'undefined') {
  window.addEventListener('click', onClickOutside)
}
</script>

<template>
  <div v-if="authorized" class="user-menu">
    <button class="avatar-btn" aria-haspopup="menu" :aria-expanded="open" @click.stop="toggle">
      <img
        v-if="user?.DiscordAvatar"
        :src="user!.DiscordAvatar"
        alt="Profile"
        class="avatar"
        referrerpolicy="no-referrer"
      >
      <span v-else class="avatar placeholder">{{
        user?.PlayerName !== null ? user?.PlayerName : user?.DiscordName
      }}</span>
    </button>

    <div v-if="open" class="menu" role="menu">
      <div class="menu-header">
        <strong>{{ user?.PlayerName !== null ? user?.PlayerName : user?.DiscordName }}</strong>
      </div>
      <BaseButton title="Profile" variant="text" @clicked="goProfile" />
      <BaseButton title="Logout" variant="text" @clicked="logout" />
    </div>
  </div>

  <div v-else>
    <RouterLink to="/login">
      Login
    </RouterLink>
  </div>
</template>

<style scoped>
.user-menu {
  position: relative;
  z-index: 9998;
}

.avatar-btn {
  background: transparent;
  border: 0;
  padding: 0;
  cursor: pointer;
  transition: transform 0.2s ease;
}

.avatar-btn:hover {
  transform: scale(1.05);
}

.avatar {
  width: 32px;
  height: 32px;
  border-radius: 9999px;
  display: block;
  border: 2px solid transparent;
  transition: border-color 0.2s ease;
}

.avatar-btn:hover .avatar {
  border-color: rgba(59, 130, 246, 0.5);
}

.avatar.placeholder {
  width: 32px;
  height: 32px;
  border-radius: 9999px;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  color: white;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 0.75rem;
}

/* Glass morphism dropdown menu */
.menu {
  position: absolute;
  right: 0;
  top: calc(100% + 12px);
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(20px);
  color: var(--fg);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 12px;
  min-width: 220px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  padding: 0.5rem;
  z-index: 9999;
  animation: menuAppear 0.2s ease-out;
}

:root[data-theme='dark'] .menu {
  background: rgba(18, 26, 45, 0.9);
  border: 1px solid rgba(255, 255, 255, 0.15);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.1);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .menu {
    background: rgba(18, 26, 45, 0.9);
    border: 1px solid rgba(255, 255, 255, 0.15);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.1);
  }
}

@keyframes menuAppear {
  from {
    opacity: 0;
    transform: translateY(-8px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.menu-header {
  padding: 0.75rem 1rem;
  color: var(--fg);
  border-bottom: 1px solid rgba(var(--color-border), 0.3);
  margin-bottom: 0.5rem;
  font-size: 0.9rem;
}

.menu-header strong {
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

/* Respect reduced motion */
@media (prefers-reduced-motion: reduce) {
  .menu,
  .avatar-btn {
    animation: none !important;
    transition: none !important;
  }
}
</style>
