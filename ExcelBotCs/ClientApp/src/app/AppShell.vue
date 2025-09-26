<script setup lang="ts">
// Global app shell
import UserMenu from '@/features/auth/UserMenu.vue'
import ThemeToggle from '@/components/ThemeToggle.vue'
import {useAuth} from '@/features/auth/useAuth'

const {isMember, isAdmin} = useAuth()

</script>

<template>
  <div class="container">
    <header class="app-header">
      <h1 class="brand">Excelsior FC</h1>
      <template v-if="isMember">
        <nav class="nav">
          <RouterLink to="/home">Home</RouterLink>
          <RouterLink to="/events">Events</RouterLink>
          <RouterLink to="/members">Members</RouterLink>
          <RouterLink to="/fights">Fights</RouterLink>
          <RouterLink to="/lottery">Lottery</RouterLink>
          <RouterLink v-if="isAdmin" to="/admin">Admin Stuff</RouterLink>
        </nav>
      </template>
      <template v-else>
        <nav class="nav">
          <RouterLink to="/home">Home</RouterLink>
          <RouterLink to="/members">Members</RouterLink>
        </nav>
      </template>
      <div class="header-actions">
        <ThemeToggle/>
        <UserMenu/>
      </div>
    </header>

    <main class="app-content">
      <RouterView/>
    </main>

    <footer class="app-footer">
      <small>© {{ new Date().getFullYear() }} Excelsior FC</small>
    </footer>
  </div>
</template>

<style scoped>
.container {
  max-width: 1100px;
  margin: 0 auto;
  padding: 1rem;
}

.app-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: .5rem 0 1rem;
  border-bottom: 1px solid var(--border);
  margin-bottom: 1rem;
}

.brand {
  font-size: 1.3rem;
  margin: 0;
}

.nav {
  display: flex;
  gap: .5rem;
}

.nav :deep(a) {
  padding: .4rem .7rem;
  border-radius: 8px;
  color: var(--fg);
  text-decoration: none;
}

.nav :deep(a.router-link-active) {
  background: color-mix(in oklab, var(--card) 80%, var(--link) 20%);
  color: var(--fg);
  border: 1px solid var(--border);
}

.header-actions {
  display: flex;
  align-items: center;
  gap: .6rem;
}

.app-footer {
  margin-top: 2rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border);
  color: var(--muted);
}
</style>