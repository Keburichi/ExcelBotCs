<script setup lang="ts">
import { onMounted } from 'vue'
import CardList from '@/components/CardList.vue'
import { useAuth } from '@/composables/useAuth'
import { useFcMembers } from '@/composables/useFcMembers'
import FcMemberCard from './FcMemberCard.vue'

const m = useFcMembers()
const { isMember } = useAuth()

onMounted(m.load)
</script>

<template>
  <section>
    <p v-if="m.error" class="error">
      {{ m.error }}
    </p>
    <div class="page-header">
      <h2 class="page-title">
        Members
      </h2>
      <span class="page-count">{{ m.members.value.length }}</span>
    </div>

    <CardList
      :items="m.members.value"
      :columns="4"
      item-key="Id"
      empty-text="No members found"
    >
      <template #item="{ item }">
        <FcMemberCard :member="item" :is-member="isMember?.valueOf()" />
      </template>
    </CardList>
  </section>
</template>

<style scoped>
.page-header {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  color: var(--fg);
}

.page-count {
  font-size: 1rem;
  font-weight: 500;
  color: var(--muted);
}
</style>
