<script setup lang="ts">
import {useAnnouncements} from "@/app/use.announcements";
import {onMounted} from "vue";

const announcements = useAnnouncements()

onMounted(announcements.load)
</script>

<template>
  <section class="home">
    <h2>Announcements</h2>
    <p v-if="announcements.error" class="error">{{ announcements.error }}</p>
    <p v-if="announcements.loading.value">Loading...</p>
    
    <div v-for="announcement in announcements.announcements.value">
      <p>{{ announcement.Content }}</p>
      <p>({{announcement.Attachments?.length ?? "0"}}) Attachments</p>
      <span v-if="announcement.Attachments?.length > 0">
        <p v-for="attachment in announcement.Attachments">
        <p>Attachment</p>
        <img :src="attachment.Url" alt="{{attachment.Name}}" />
      </p>
      </span>
      <span>{{announcement.Author}} - {{announcement.Timestamp}}</span>
    </div>
  </section>
</template>

<style scoped>

</style>