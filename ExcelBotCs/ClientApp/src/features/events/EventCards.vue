<script setup lang="ts">
import type {FCEvent} from './events.types'

const props = defineProps<{
  items: FCEvent[]
  editId: string | null
  editBuffer: FCEvent
  canEdit?: boolean
}>()

const emit = defineEmits<{
  'start-edit': [event: FCEvent]
  'cancel-edit': []
  'save-edit': [],
  'delete-event': [event: FCEvent]
}>()
</script>

<template>
  <div id="events-container">
    <div v-for="e in props.items" :key="e.Id" class="event-card">
      <div class="event-thumb" :style="e.PictureUrl ? {'--event-image': 'url(' + e.PictureUrl + ')'} : {}"></div>
      <div class="event-card-content">
        <template v-if="props.editId === e.Id">
          <h3>Editing: {{ e.Name }}</h3>
          <label>Name</label>
          <input v-model="props.editBuffer.Name" type="text"/>

          <label>Description</label>
          <textarea v-model="props.editBuffer.Description" rows="4"/>

          <label>Discord Message</label>
          <textarea v-model="props.editBuffer.DiscordMessage" rows="3"/>

          <label>Picture URL</label>
          <input v-model="props.editBuffer.PictureUrl" type="url"/>

          <div class="actions">
            <button class="btn" @click="emit('save-edit')">Save</button>
            <button class="btn secondary" @click="emit('cancel-edit')">Cancel</button>
          </div>
        </template>
        <template v-else>
          <h3><b>{{ e.Name }}</b></h3>
          <p>{{ e.Description }}</p>
          <p>Organized by: <i>{{ e.Organizer }}</i></p>
          <div class="actions" v-if="props.canEdit">
            <button class="btn" @click="emit('start-edit', e)">Edit</button>
            <button class="btn danger" @click="emit('delete-event', e)">Delete</button>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
#events-container {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1rem;
}

.event-card {
  border: 1px solid #ccc;
  text-align: left;
  border-radius: 8px;
  transition: 0.3s;
  box-shadow: 0 4px 8px 0 rgba(204, 204, 204, 0.2);
}

.event-card:hover {
  box-shadow: 0 8px 16px 0 rgba(204, 204, 204, 0.2);
}

.event-thumb {
  border-radius: 8px 8px 0 0;
  width: 100%;
  aspect-ratio: 16/9;
  background-image: var(--event-image, url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='800' height='450' viewBox='0 0 800 450'><rect width='800' height='450' fill='%23f3f4f6'/><g fill='%2399a1a8' font-family='Segoe UI, Roboto, Arial, sans-serif' text-anchor='middle'><text x='400' y='230' font-size='28'>No image</text></g></svg>"));
  background-size: cover;
  background-position: center;
  background-color: #f3f4f6;
}

.event-card-content {
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.event-card-content h3 {
  margin: 0;
  font-size: 1.2rem;
  min-height: 2.5rem;
  max-height: 2.5rem;
}

.actions {
  display: flex;
  gap: 0.5rem;
}

</style>