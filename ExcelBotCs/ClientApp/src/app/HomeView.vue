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
          <h2 class="intro-title text-2xl sm:text-3xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
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
}

.intro-text {
  margin: 0;
  color: var(--muted);
}
</style>
