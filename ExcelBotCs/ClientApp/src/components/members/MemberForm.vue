<script setup lang="ts">
import type { Member } from './members.types'
import BaseButton from '@/components/BaseButton.vue'

const emit = defineEmits<{ submit: [] }>()
const model = defineModel<Member>({ required: true })
</script>

<template>
  <form @submit.prevent="emit('submit')">
    <div class="row">
      <label>
        Name
        <input v-model="model.PlayerName" required>
      </label>
      <label>
        Player Name
        <input v-model="model.PlayerName">
      </label>
      <label class="checkbox">
        <input v-model="model.Subbed" type="checkbox"> Subbed
      </label>
    </div>
    <BaseButton title="Save" type="submit" />
  </form>
</template>
