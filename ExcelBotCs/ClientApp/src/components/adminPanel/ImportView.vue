<script setup lang="ts">
import BaseButton from '@/components/BaseButton.vue'
import { useAdmin } from '@/composables/useAdmin'
import { useAuth } from '@/composables/useAuth'

const admin = useAdmin()
const { isAdmin } = useAuth()
</script>

<template>
  <h2 class="text-3xl font-bold mt-4 mb-4">
    Import things into the database
  </h2>

  <h3 class="text-2xl font-bold mt-4 mb-4">
    Import Fights
  </h3>
  <BaseButton
    v-if="isAdmin"
    title="Import Fights"
    @clicked="admin.importFights()"
  />
</template>

<style scoped>

</style>
