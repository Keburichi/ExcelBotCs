<script setup lang="ts">
import { onMounted } from 'vue'
import CardList from '@/components/CardList.vue'
import { useAuth } from '@/composables/useAuth'
import { useFcMembers } from '@/composables/useFcMembers'
import FcMemberCard from './FcMemberCard.vue'

const m = useFcMembers()
const { isAdmin, ensureAuth, isMember } = useAuth()

onMounted(m.load)
</script>

<template>
  <section class="home">
    <p v-if="m.error" class="error">
      {{ m.error }}
    </p>
    <div class="page-header">
      <h2 class="page-title">
        Members ({{ m.members.value.length }})
      </h2>
    </div>

    <CardList
      :items="m.members.value"
      :columns="4"
      item-key="Id"
    >
      <template #item="{ item }">
        <FcMemberCard :member="item" :is-member="isMember?.valueOf()" />
      </template>
    </CardList>
  </section>
</template>

<style scoped>
/* Page header */
.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  color: transparent;
  letter-spacing: -0.02em;
}
</style>
