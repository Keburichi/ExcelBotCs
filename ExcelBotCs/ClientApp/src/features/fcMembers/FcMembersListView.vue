<script setup lang="ts">
import {onMounted} from 'vue'
import {useFcMembers} from './useFcMembers'
import FcMemberCard from './FcMemberCard.vue'
import {useAuth} from "@/features/auth/useAuth";
import CardList from "@/components/CardList.vue";

const m = useFcMembers()
const {isAdmin, ensureAuth, isMember} = useAuth()

onMounted(m.load)
</script>

<template>
  <section class="home">
    <p v-if="m.error" class="error">{{ m.error }}</p>
    <h2>Members ({{m.members.value.length}})</h2>
    
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