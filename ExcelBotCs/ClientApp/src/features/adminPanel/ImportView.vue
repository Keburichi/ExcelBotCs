<script setup lang="ts">

import {useAdmin} from "@/composables/useAdmin";
import {useAuth} from "@/composables/useAuth";

const admin = useAdmin()
const {isAdmin} = useAuth()

</script>

<template>

  <h2>Import things into the database</h2>

  <h3>Import Fights</h3>
  <button v-if="isAdmin" class="btn" @click="admin.importFights()">Import Fights</button>
  <h3>Import Discord Members</h3>
  <button v-if="isAdmin" class="btn" @click="admin.importMembers()">Import Discord Members</button>
  <h3>Import Fc (Lodestone) Members</h3>
  <button v-if="isAdmin" class="btn" @click="admin.importFcMembers()">Import FC Members</button>
  <h3>Import Roles </h3>
  <button v-if="isAdmin" class="btn" @click="admin.importRoles()">Import Roles</button>

</template>

<style scoped>

</style>