<script setup lang="ts">
import BaseButton from '@/components/BaseButton.vue'
import { useAdmin } from '@/composables/useAdmin'
import { useAuth } from '@/composables/useAuth'

const admin = useAdmin()
const { isAdmin } = useAuth()
</script>

<template>
  <h2 class="section-heading">
    Import things into the database
  </h2>

  <h3 class="sub-heading">
    Import Fights
  </h3>
  <BaseButton
    v-if="isAdmin"
    title="Import Fights"
    @clicked="admin.importFights()"
  />
</template>

<style scoped>
.section-heading { font-size: 1.875rem; font-weight: 700; margin: 1rem 0; }
.sub-heading { font-size: 1.5rem; font-weight: 700; margin: 1rem 0; }
</style>
