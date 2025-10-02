<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import CardList from '@/components/CardList.vue'
import EventCard from '@/components/events/EventCard.vue'
import { useAuth } from '@/composables/useAuth'
import { useEvents } from '@/composables/useEvents'

const e = useEvents()
const { isAdmin, isMember } = useAuth()
const router = useRouter()

function goCreate() {
  router.push({ name: 'event-create' })
}

function goEdit(event: any) {
  router.push({ name: 'event-edit', params: { id: event.Id } })
}

onMounted(e.load)
</script>

<template>
  <section class="home">
    <h2>Upcoming FC Events ({{ e.events.value.length }})</h2>
    <p v-if="e.error" class="error">
      {{ e.error }}
    </p>

    <div v-if="isAdmin" class="container">
      <BaseButton v-if="isAdmin" title="Create Event" state="primary" size="medium" @clicked="goCreate" />
      <button v-if="isAdmin?.valueOf()" class="btn" @click="goCreate">
        + Create Event
      </button>
    </div>

    <CardList :items="e.events.value" :columns="2" item-key="Id">
      <template #item="{ item }">
        <EventCard
          :fc-event="item"
          :is-member="isMember?.valueOf()"
          :is-admin="isAdmin?.valueOf()"
          @start-edit="goEdit"
          @cancel-edit="e.cancelEdit"
          @save-edit="e.save"
          @delete-event="e.deleteEvent"
        />
      </template>
    </CardList>
  </section>
</template>
