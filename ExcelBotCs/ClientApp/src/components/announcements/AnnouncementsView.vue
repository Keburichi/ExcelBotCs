<script setup lang="ts">
import { onMounted } from 'vue'
import DiscordMessageRenderer from '@/components/DiscordMessageRenderer.vue'
import { useAnnouncements } from '@/composables/useAnnouncements'

const announcements = useAnnouncements()

onMounted(announcements.load)
</script>

<template>
  <section class="home">
    <h2>Announcements</h2>
    <p v-if="announcements.error" class="error">
      {{ announcements.error }}
    </p>
    <p v-if="announcements.loading.value">
      Loading...
    </p>

    <div v-for="announcement in announcements.announcements.value" :key="announcement.Timestamp" class="announcement-card">
      <div class="announcement-header">
        <span class="announcement-author">{{ announcement.Author }}</span>
        <span class="announcement-timestamp">{{ new Date(announcement.Timestamp).toLocaleString() }}</span>
      </div>

      <DiscordMessageRenderer
        :content="announcement.Content"
        :attachments="announcement.Attachments"
      />
    </div>
  </section>
</template>

<style scoped>
.announcement-card {
  background: var(--card);
  border: 1px solid var(--card-border);
  border-radius: 12px;
  padding: 1rem;
  margin-bottom: 1rem;
  box-shadow: var(--elev);
}

.announcement-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid var(--border);
}

.announcement-author {
  font-weight: 600;
  color: var(--fg);
  font-size: 0.95rem;
}

.announcement-timestamp {
  color: var(--muted);
  font-size: 0.85rem;
}

@media (max-width: 640px) {
  .announcement-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
  }
}
</style>
