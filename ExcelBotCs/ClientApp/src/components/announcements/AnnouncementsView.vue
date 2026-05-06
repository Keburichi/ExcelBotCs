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
        <div class="announcement-author">
          <img
            v-if="announcement.AuthorAvatarUrl"
            :alt="announcement.Author"
            :src="announcement.AuthorAvatarUrl"
            class="announcement-avatar"
          >
          <div v-else class="announcement-avatar announcement-avatar--fallback">
            {{ announcement.Author.charAt(0) }}
          </div>
          <span class="announcement-author-name">{{ announcement.Author }}</span>
        </div>
        <span class="announcement-timestamp">{{ new Date(announcement.Timestamp).toLocaleString() }}</span>
      </div>

      <DiscordMessageRenderer
        :attachments="announcement.Attachments"
        :content="announcement.Content"
        :mentions="announcement.Mentions"
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
  backdrop-filter: blur(24px);
  border-color: rgba(59, 130, 246, 0.3);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.15),
  0 4px 16px rgba(0, 0, 0, 0.1),
  inset 0 1px 0 rgba(255, 255, 255, 0.6);
}

:root[data-theme='dark'] .announcement-card:hover {
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
  0 4px 16px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .announcement-card:hover {
    border-color: rgba(59, 130, 246, 0.4);
    box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
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
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.announcement-avatar {
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.announcement-avatar--fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(59, 130, 246, 0.2);
  color: rgb(59, 130, 246);
  font-weight: 700;
  font-size: 1rem;
}

.announcement-author-name {
  font-weight: 700;
  color: var(--fg);
  font-size: 1.1rem;
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
