<script setup lang="ts">
import {onMounted, ref} from 'vue'
import {useRouter} from 'vue-router'
import {useAuth} from '@/features/auth/useAuth'

const {authorized, user, ensureAuth, logout, loadMe} = useAuth()
const open = ref(false)
const router = useRouter()

onMounted(() => {
  void loadMe()
})

function toggle() {
  open.value = !open.value
}

function goProfile() {
  open.value = false;
  router.push('/profile')
}

function onClickOutside(e: MouseEvent) {
  const target = e.target as HTMLElement | null
  if (!target) return
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
  <div class="user-menu" v-if="authorized">
    <button class="avatar-btn" @click.stop="toggle" aria-haspopup="menu" :aria-expanded="open">
      <img
          v-if="user?.DiscordAvatar"
          :src="user!.DiscordAvatar"
          alt="Profile"
          class="avatar"
          referrerpolicy="no-referrer"/>
      <span v-else class="avatar placeholder">{{
          user?.PlayerName !== null ? user?.PlayerName : user?.DiscordName
        }}</span>
    </button>

    <div v-if="open" class="menu" role="menu">
      <div class="menu-header">
        <strong>{{ user?.PlayerName !== null ? user?.PlayerName : user?.DiscordName }}</strong>
      </div>
      <button role="menuitem" class="menu-entry" @click="goProfile">Profile</button>
      <button role="menuitem" class="menu-entry" @click="logout">Logout</button>
    </div>
  </div>

  <div v-else>
    <RouterLink to="/login">Login</RouterLink>
  </div>
</template>

<style scoped>
.user-menu {
  position: relative;
}

.avatar-btn {
  background: transparent;
  border: 0;
  padding: 0;
  cursor: pointer;
}

.avatar {
  width: 32px;
  height: 32px;
  border-radius: 9999px;
  display: block;
}

.avatar.placeholder {
  width: 32px;
  height: 32px;
  border-radius: 9999px;
  background: var(--border);
  color: var(--muted);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
}

.menu {
  position: absolute;
  right: 0;
  top: calc(100% + 8px);
  background: var(--card);
  color: var(--fg);
  border: 1px solid var(--card-border);
  border-radius: 10px;
  min-width: 200px;
  box-shadow: var(--elev);
  padding: .25rem;
  z-index: 10;
}

.menu > button {
  width: 100%;
  text-align: left;
  background: transparent;
  color: var(--fg);
  border: 0;
  padding: .6rem .75rem;
  cursor: pointer;
  border-radius: 8px;
}

.menu > button:hover {
  background: color-mix(in oklab, var(--card) 85%, var(--link) 15%);
}

.menu-header {
  padding: .6rem .75rem;
  color: var(--muted);
  border-bottom: 1px solid var(--border);
  margin-bottom: .25rem;
}

.menu-entry {
  display: block;
  width: 100%;
  text-align: left;
  padding: .6rem .75rem;
  color: var(--fg);
  text-decoration: none;
}

</style>