<script lang="ts" setup>
import { useAuth } from '@/composables/useAuth'
import RulesView from '@/app/RulesView.vue'
import AnnouncementsView from '@/components/announcements/AnnouncementsView.vue'

const { isMember } = useAuth()
</script>

<template>
  <div class="home">
    <div class="home-columns">
      <RulesView class="home-rules" />
      <AnnouncementsView class="home-feed" />
    </div>

    <nav v-if="isMember" class="home-nav">
      <RouterLink to="/events" class="home-nav-item">
        <span class="home-nav-label">Events</span>
        <span class="home-nav-desc">Upcoming FC gatherings</span>
      </RouterLink>
      <RouterLink to="/members" class="home-nav-item">
        <span class="home-nav-label">Members</span>
        <span class="home-nav-desc">FC directory</span>
      </RouterLink>
      <RouterLink to="/fights" class="home-nav-item">
        <span class="home-nav-label">Fights</span>
        <span class="home-nav-desc">Guides and resources</span>
      </RouterLink>
      <RouterLink to="/lottery" class="home-nav-item">
        <span class="home-nav-label">Lottery</span>
        <span class="home-nav-desc">Weekly drawings</span>
      </RouterLink>
    </nav>
  </div>
</template>

<style scoped>
.home-columns {
  display: grid;
  grid-template-columns: 1fr 1.6fr;
  gap: 2.5rem;
  align-items: start;
}

.home-nav {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.5rem;
  margin-top: 3rem;
  padding-top: 2rem;
  border-top: 1px solid var(--border);
}

.home-nav-item {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  padding: 0.75rem 1rem;
  border-radius: 12px;
  text-decoration: none;
  transition: background 200ms ease;
}

.home-nav-item:hover {
  background: color-mix(in oklab, var(--link) 8%, transparent);
  text-decoration: none;
}

.home-nav-label {
  font-weight: 600;
  font-size: 0.95rem;
  color: var(--link);
}

.home-nav-desc {
  font-size: 0.85rem;
  color: var(--muted);
  line-height: 1.4;
}

@media (max-width: 768px) {
  .home-columns {
    grid-template-columns: 1fr;
    gap: 2.5rem;
  }

  .home-feed { order: 1; }
  .home-rules { order: 2; }

  .home-nav {
    grid-template-columns: repeat(2, 1fr);
    margin-top: 2rem;
    padding-top: 1.5rem;
  }
}
</style>
