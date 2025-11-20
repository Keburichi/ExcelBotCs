<script setup lang="ts">
import { onMounted, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import CardList from '@/components/CardList.vue'
import { useAuth } from '@/composables/useAuth'
import { useMembers } from '@/composables/useMembers'
import MemberCard from './MemberCard.vue'
import MembersTable from './MembersTable.vue'
import MemberStatistics from './MemberStatistics.vue'

const m = useMembers()
const { isAdmin, isMember } = useAuth()

type ViewMode = 'card' | 'list'
const viewMode = ref<ViewMode>('card')

function toggleView(mode: ViewMode) {
  viewMode.value = mode
}

onMounted(m.load)
</script>

<template>
  <section class="home">
    <div class="header-section">
      <h2 class="text-3xl font-bold">
        Members
      </h2>

      <div class="view-toggle">
        <BaseButton
          :variant="viewMode === 'card' ? 'elevated' : 'outlined'"
          icon="📇"
          size="small"
          title="Card View"
          @clicked="toggleView('card')"
        />
        <BaseButton
          :variant="viewMode === 'list' ? 'elevated' : 'outlined'"
          icon="📋"
          size="small"
          title="List View"
          @clicked="toggleView('list')"
        />
      </div>
    </div>

    <MemberStatistics :members="m.members.value" />

    <p v-if="m.error" class="error">
      {{ m.error }}
    </p>

    <!-- Card View -->
    <div v-if="viewMode === 'card'" class="card-view">
      <h3>All Members ({{ m.members.value.length }})</h3>
      <CardList
        :columns="4"
        :items="m.members.value"
        item-key="Id"
      >
        <template #item="{ item }">
          <MemberCard :is-member="isMember?.valueOf()" :member="item" />
        </template>
      </CardList>
    </div>

    <!-- List View -->
    <div v-if="viewMode === 'list'" class="list">
      <h3>All Members ({{ m.members.value.length }})</h3>
      <MembersTable
        v-model="m.editBuffer"
        :items="m.members.value"
        :edit-id="m.editId.value"
        :can-edit="isAdmin?.valueOf()"
        :is-member="isMember?.valueOf()"
        @start-edit="m.startEdit"
        @cancel-edit="m.cancelEdit"
        @save-edit="m.save"
      />
    </div>
  </section>
</template>

<style scoped>
.header-section {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
}

.view-toggle {
  display: flex;
  gap: 0.5rem;
}

.card-view,
.list {
  animation: fadeIn 0.2s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}
</style>
