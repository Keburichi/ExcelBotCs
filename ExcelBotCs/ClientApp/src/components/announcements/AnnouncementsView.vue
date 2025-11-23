<script lang="ts" setup>
import { onMounted } from 'vue'
import DiscordMessageRenderer from '@/components/DiscordMessageRenderer.vue'
import { useAnnouncements } from '@/composables/useAnnouncements'

const announcements = useAnnouncements()

onMounted(announcements.load)
</script>

<template>
  <section class="home container">
    <h2>Announcements</h2>
    <p v-if="announcements.error" class="error">
      {{ announcements.error }}
    </p>
    <p v-if="announcements.loading.value">
      Loading...
    </p>

    <div
      v-for="announcement in announcements.announcements.value" :key="announcement.Timestamp"
      class="announcement-card"
    >
      <div class="announcement-header">
        <span class="announcement-author">{{ announcement.Author }}</span>
        <span class="announcement-timestamp">{{ new Date(announcement.Timestamp).toLocaleString() }}</span>
      </div>

      <DiscordMessageRenderer
        :attachments="announcement.Attachments"
        :content="announcement.Content"
      />
    </div>
  </section>
</template>

<style scoped>
.announcement-card {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 16px;
  padding: 1.5rem;
  margin-bottom: 1rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

:root[data-theme='dark'] .announcement-card {
  background: rgba(18, 26, 45, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .announcement-card {
    background: rgba(18, 26, 45, 0.7);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.announcement-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.12),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
}

:root[data-theme='dark'] .announcement-card:hover {
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .announcement-card:hover {
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.announcement-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid rgba(var(--color-border), 0.3);
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
