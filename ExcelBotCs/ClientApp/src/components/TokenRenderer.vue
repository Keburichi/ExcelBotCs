<script lang="ts" setup>
defineProps<{
  tokens: any[]
  revealedSpoilers: Set<number>
  formatTimestamp: (timestamp: number, format: string) => string
  toggleSpoiler: (index: number) => void
}>()
</script>

<template>
  <template v-for="(token, tokenIdx) in tokens" :key="tokenIdx">
    <span v-if="token.type === 'text'">{{ token.content }}</span>
    <strong v-else-if="token.type === 'bold'">{{ token.content }}</strong>
    <em v-else-if="token.type === 'italic'">{{ token.content }}</em>
    <code v-else-if="token.type === 'code'" class="discord-inline-code">{{ token.content }}</code>
    <span
      v-else-if="token.type === 'timestamp'"
      class="discord-timestamp"
    >{{ formatTimestamp(token.timestamp, token.format) }}</span>
    <span
      v-else-if="token.type === 'spoiler'"
      :class="{ revealed: revealedSpoilers.has(token.index) }"
      class="discord-spoiler"
      role="button"
      tabindex="0"
      @click="toggleSpoiler(token.index)"
      @keydown.enter.space.prevent="toggleSpoiler(token.index)"
    >
      {{ token.content }}
    </span>
    <span
      v-else-if="token.type === 'userMention'"
      class="discord-mention discord-mention--user"
    >@{{ token.name ?? 'Unknown User' }}</span>
    <span
      v-else-if="token.type === 'roleMention'"
      class="discord-mention discord-mention--role"
    >@{{ token.name ?? 'Unknown Role' }}</span>
    <span
      v-else-if="token.type === 'channelMention'"
      class="discord-mention discord-mention--channel"
    >#{{ token.name ?? 'Unknown Channel' }}</span>
    <img
      v-else-if="token.type === 'emote'" :alt="token.name"
      :src="`https://cdn.discordapp.com/emojis/${token.id}.${token.animated ? 'gif' : 'webp'}`"
      class="discord-emote"
    >
    <a
      v-else-if="token.type === 'link'" :href="token.url" class="discord-link" rel="noopener noreferrer"
      target="_blank"
    >{{ token.display }}</a>
  </template>
</template>
