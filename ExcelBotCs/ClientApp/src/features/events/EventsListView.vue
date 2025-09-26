<script setup lang="ts">
import {onMounted} from 'vue'
import {useEvents} from './useEvents'
import EventCards from './EventCards.vue'
import {useAuth} from "@/features/auth/useAuth";
import {useRouter} from 'vue-router'
import EventCard from "@/features/events/EventCard.vue";
import CardList from "@/components/CardList.vue";
import FightCard from "@/features/fights/FightCard.vue";

const e = useEvents()
const {isAdmin, isMember} = useAuth()
const router = useRouter()

function goCreate() {
  router.push({name: 'event-create'})
}

onMounted(e.load)
</script>

<template>
  <section class="home">
    <h2>Upcoming FC Events ({{e.events.value.length}})</h2>
    <p v-if="e.error" class="error">{{ e.error }}</p>

    <div v-if="isAdmin" class="container">
      <button v-if="isAdmin?.valueOf()" class="btn" @click="goCreate">+ Create Event</button>
    </div>

    <CardList :items="e.events.value" :columns="2" item-key="Id">
      <template #item="{ item }">
        <EventCard :event="item"
                   :isMember="isMember?.valueOf()"
                   :isAdmin="isAdmin?.valueOf()"
                   @start-edit="e.startEdit"
                   @cancel-edit="e.cancelEdit"
                   @save-edit="e.save"
                   @delete-event="e.deleteEvent"/>
      </template>
    </CardList>
    
<!--    <div class="list">-->
<!--      <h3>All Events ({{ e.events.value.length }})</h3>-->

<!--      <div class="cards_container&#45;&#45;medium">-->
<!--        <EventCard v-for="e in e.events.value" :key="e.Id"-->
<!--                   :event="e"-->
<!--                   :is-member="isMember?.valueOf()"-->
<!--        />-->
<!--      </div>-->

<!--      <EventCards-->
<!--          :items="e.events.value"-->
<!--          :edit-id="e.editId.value"-->
<!--          :edit-buffer="e.editBuffer"-->
<!--          :can-edit="isAdmin?.valueOf()"-->
<!--          @start-edit="e.startEdit"-->
<!--          @cancel-edit="e.cancelEdit"-->
<!--          @save-edit="e.save"-->
<!--          @delete-event="e.deleteEvent"-->
<!--      />-->
<!--    </div>-->
  </section>
</template>