<script lang="ts" setup>
import RulesView from '@/app/RulesView.vue'
import AnnouncementsView from '@/components/announcements/AnnouncementsView.vue'
</script>

<template>
  <section class="home">
    <!-- Intro card -->
    <div class="container">
      <div class="card intro-card">
        <div class="card__body">
          <h2 class="intro-title">
            Welcome!
          </h2>
          <p class="intro-text">
            On this website you can find upcoming FC Events, helpful resources for fights and interact with our Discord
            Bot in a graphical way.
          </p>
        </div>
      </div>
    </div>

    <RulesView />
    <AnnouncementsView />
  </section>
</template>

<style scoped>
/* Subtle spacing between sections to improve visual rhythm */
.home :deep(.container) {
  /* no-op: ensures specificity without altering base container */
}

.intro-card {
  margin-bottom: 1rem;
}

.intro-title {
  margin: 0 0 .25rem 0;
  color: var(--fg) !important;
  font-weight: 700;
  font-size: 1.5rem;
  letter-spacing: -0.025em;
}

@media (min-width: 640px) {
  .intro-title { font-size: 1.875rem; }
}

.intro-text {
  margin: 0;
  color: var(--fg);
  opacity: 0.8;
}
</style>
