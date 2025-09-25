<script setup lang="ts">
import {reactive, ref} from 'vue'
import {useRouter} from 'vue-router'
import {EventsApi} from './events.api'
import type {FCEvent} from './events.types'
import {useAuth} from '@/features/auth/useAuth'

const router = useRouter()
const {user, isAdmin, ensureAuth, loadMe} = useAuth()

const loading = ref(false)
const error = ref('')

const form = reactive<FCEvent>({
  Id: '',
  Name: '',
  Description: '',
  DiscordMessage: '',
  PictureUrl: '',
  Organizer: '' // will be filled from current user on submit, server sets Author
})

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await loadMe()
    if (!isAdmin.value) {
      error.value = 'You do not have permission to create events.'
      return
    }

    // Organizer is computed from Author on the backend; we can set it for display
    form.Organizer = user.value?.name ?? ''

    await EventsApi.create(form)
    await router.push({name: 'events'})
  } catch (e: any) {
    error.value = e?.message || 'Failed to create event'
  } finally {
    loading.value = false
  }
}

function cancel() {
  router.push({name: 'events'})
}
</script>

<template>
  <section class="page create-event">
    <h2>Create FC Event</h2>
    <p v-if="error" class="error">{{ error }}</p>
    <form @submit.prevent="submit" class="form">
      <div class="form-row">
        <label>Name</label>
        <input v-model="form.Name" type="text" required placeholder="Event name"/>
      </div>
      <div class="form-row">
        <label>Description</label>
        <textarea v-model="form.Description" rows="5" placeholder="Describe the event"></textarea>
      </div>
      <div class="form-row">
        <label>Discord Message</label>
        <textarea v-model="form.DiscordMessage" rows="4" placeholder="Message to post on Discord"></textarea>
      </div>
      <div class="form-row">
        <label>Picture URL (optional)</label>
        <input v-model="form.PictureUrl" type="url" placeholder="https://..."/>
      </div>
      <div class="form-row">
        <label>Organizer</label>
        <input :value="user?.name || ''" type="text" disabled/>
      </div>
      <div class="actions">
        <button class="btn" type="submit" :disabled="loading">{{ loading ? 'Creating...' : 'Create' }}</button>
        <button class="btn secondary" type="button" @click="cancel" :disabled="loading">Cancel</button>
      </div>
    </form>
  </section>
</template>

<style scoped>
.page {
  max-width: 720px;
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 12px 0;
}

.actions {
  display: flex;
  gap: 8px;
  margin-top: 16px;
}

.error {
  color: #c62828;
}
</style>
