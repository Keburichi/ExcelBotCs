<script setup lang="ts">
import { useAuth } from '@/composables/useAuth'

// The Admin link only appears for admins; the page itself lands in #114, so the link is the contract between the
// member views and the admin view.
const { isAdmin } = useAuth()
</script>

<template>
  <nav class="lottery-tabs" aria-label="Lottery sections">
    <RouterLink
      to="/lottery"
      class="lottery-tab"
      exact-active-class="lottery-tab--active"
    >
      Play
    </RouterLink>
    <RouterLink
      to="/lottery/history"
      class="lottery-tab"
      active-class="lottery-tab--active"
    >
      History
    </RouterLink>
    <RouterLink
      v-if="isAdmin"
      to="/lottery/admin"
      class="lottery-tab lottery-tab--admin"
      active-class="lottery-tab--active"
    >
      Admin
    </RouterLink>
  </nav>
</template>

<style scoped>
.lottery-tabs {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  width: fit-content;
  max-width: 100%;
  margin-bottom: 1.5rem;
  padding: 0.375rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--muted-bg);
}

.lottery-tab {
  padding: 0.5rem 1rem;
  border-radius: 9px;
  color: var(--muted);
  font-size: 0.875rem;
  font-weight: 600;
  text-decoration: none;
  transition: background 200ms ease, color 200ms ease;
}

.lottery-tab:hover {
  color: var(--fg);
}

.lottery-tab:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.lottery-tab--active {
  color: var(--fg);
  background: color-mix(in oklab, var(--link) 14%, var(--card));
  box-shadow: inset 0 0 0 1px color-mix(in oklab, var(--link) 24%, var(--border));
}

.lottery-tab--admin {
  margin-left: 0.5rem;
  color: var(--link);
}

@media (max-width: 480px) {
  .lottery-tabs {
    width: 100%;
  }

  .lottery-tab {
    flex: 1;
    text-align: center;
  }

  .lottery-tab--admin {
    margin-left: 0;
  }
}
</style>
