<script setup lang="ts">
import {useAdmin} from "@/features/adminPanel/useAdmin";
import {useAuth} from "@/features/auth/useAuth";

const admin = useAdmin()
const {isAdmin} = useAuth()

</script>

<template>

  <h2>Import things into the database</h2>

  <h3>Import Fights</h3>
  <button v-if="isAdmin" class="btn" @click="admin.importFights()">Import Fights</button>
  <h3>Import Members</h3>
  <button v-if="isAdmin" class="btn" @click="admin.importMembers()">Import Members</button>
  <h3>Import Roles </h3>
  <button v-if="isAdmin" class="btn" @click="admin.importRoles()">Import Roles</button>

</template>

<style scoped>

</style>