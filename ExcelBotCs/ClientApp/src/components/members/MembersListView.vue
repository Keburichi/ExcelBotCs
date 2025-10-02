<script setup lang="ts">
import { onMounted } from 'vue'
import CardList from '@/components/CardList.vue'
import { useAuth } from '@/composables/useAuth'
import { useMembers } from '@/composables/useMembers'
import MemberCard from './MemberCard.vue'
import MembersTable from './MembersTable.vue'

const m = useMembers()
const { isAdmin, isMember } = useAuth()

onMounted(m.load)
</script>

<template>
  <section class="home">
    <h2>Members</h2>

    <CardList
      :items="m.members.value"
      :columns="4"
      item-key="Id"
    >
      <template #item="{ item }">
        <MemberCard :member="item" :is-member="isMember?.valueOf()" />
      </template>
    </CardList>

    <p v-if="m.error" class="error">
      {{ m.error }}
    </p>

    <div class="list">
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
