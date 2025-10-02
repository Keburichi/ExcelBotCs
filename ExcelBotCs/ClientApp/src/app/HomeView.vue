<script setup lang="ts">
import RulesView from '@/app/RulesView.vue'
import AnnouncementsView from '@/components/announcements/AnnouncementsView.vue'
</script>

<template>
  <section class="home">
    <h2>Welcome!</h2>
    <p>On this website you can find upcoming FC Events, helpful resources for fights and interact with our Discord Bot in a graphical way.</p>

    <RulesView />
    <AnnouncementsView />
  </section>
</template>

<style scoped>

</style>
