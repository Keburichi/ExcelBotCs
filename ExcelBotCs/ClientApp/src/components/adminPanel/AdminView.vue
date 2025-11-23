<script setup lang="ts">
import ImportView from '@/components/adminPanel/ImportView.vue'
import MemberRolesView from '@/components/adminPanel/MemberRolesView.vue'
</script>

<template>
  <h2>Admin Panel</h2>
  <h3>
    Import
  </h3>
  <ImportView />

  <MemberRolesView />
</template>

<style scoped>
h2, h3 {
  color: var(--fg);
}

h2 {
  font-size: 2rem;
  font-weight: 700;
  margin-bottom: 1.5rem;
}

h3 {
  font-size: 1.5rem;
  font-weight: 600;
  margin-top: 2rem;
  margin-bottom: 1rem;
}
</style>
