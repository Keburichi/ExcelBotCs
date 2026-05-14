<script lang="ts" setup>
import { onMounted } from 'vue'
import DiscordMessageRenderer from '@/components/DiscordMessageRenderer.vue'
import { useAnnouncements } from '@/composables/useAnnouncements'

const announcements = useAnnouncements()

onMounted(announcements.load)
</script>

<template>
  <section>
    <h2 class="heading">Announcements</h2>

    <p v-if="announcements.error.value" class="error">
      {{ announcements.error.value }}
    </p>

    <div v-if="announcements.loading.value" class="feed">
      <div v-for="i in 3" :key="i" class="entry">
        <div class="entry-meta">
          <div class="skel skel-avatar" />
          <div class="skel skel-name" />
          <div class="skel skel-time" />
        </div>
        <div class="skel skel-line skel-line--full" />
        <div class="skel skel-line skel-line--mid" />
        <div class="skel skel-line skel-line--short" />
      </div>
    </div>

    <div v-else class="feed">
      <article
        v-for="a in announcements.announcements.value"
        :key="a.Timestamp"
        class="entry"
      >
        <div class="entry-meta">
          <img
            v-if="a.AuthorAvatarUrl"
            :alt="a.Author"
            :src="a.AuthorAvatarUrl"
            class="entry-avatar"
          >
          <span v-else class="entry-avatar entry-avatar--fallback">
            {{ a.Author.charAt(0) }}
          </span>
          <span class="entry-author">{{ a.Author }}</span>
          <span class="entry-time">{{ new Date(a.Timestamp).toLocaleString() }}</span>
        </div>

        <div class="entry-body">
          <DiscordMessageRenderer
            :attachments="a.Attachments"
            :content="a.Content"
            :mentions="a.Mentions"
          />
        </div>
      </article>
    </div>
  </section>
</template>

<style scoped>
.heading {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--fg);
  margin-bottom: 1.25rem;
  letter-spacing: -0.01em;
  line-height: 1.4;
}

.error {
  color: var(--danger);
  margin-bottom: 1rem;
}

.feed {
  display: flex;
  flex-direction: column;
}

.entry {
  padding: 1.25rem 0;
  border-bottom: 1px solid var(--border);
}

.entry:first-child {
  padding-top: 0;
}

.entry:last-child {
  border-bottom: none;
}

.entry-meta {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.625rem;
}

.entry-avatar {
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.entry-avatar--fallback {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in oklab, var(--link) 20%, var(--bg) 80%);
  color: var(--link);
  font-weight: 700;
  font-size: 0.75rem;
}

.entry-author {
  font-weight: 600;
  font-size: 0.9rem;
  color: var(--fg);
}

.entry-time {
  color: var(--muted);
  font-size: 0.9rem;
  margin-left: auto;
}

.entry-body {
  line-height: 1.6;
  color: var(--fg);
}

/* Skeleton loading */
.skel {
  background: var(--border);
  border-radius: 6px;
  animation: pulse 1.5s ease-in-out infinite;
}

.skel-avatar {
  width: 1.75rem;
  height: 1.75rem;
  border-radius: 50%;
  flex-shrink: 0;
}

.skel-name {
  width: 7rem;
  height: 0.85rem;
}

.skel-time {
  width: 3.5rem;
  height: 0.7rem;
  margin-left: auto;
}

.skel-line {
  height: 0.85rem;
  margin-top: 0.5rem;
}

.skel-line--full { width: 100%; }
.skel-line--mid { width: 85%; }
.skel-line--short { width: 55%; }

@keyframes pulse {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.7; }
}

@media (max-width: 640px) {
  .entry-meta {
    flex-wrap: wrap;
  }

  .entry-time {
    margin-left: 0;
    flex-basis: 100%;
    padding-left: 2.25rem;
  }
}
</style>
