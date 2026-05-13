<script setup lang="ts">
import type { MentionData } from '@/app/announcements.types'
import { computed, ref } from 'vue'
import TokenRenderer from '@/components/TokenRenderer.vue'

interface MessageAttachment {
  Name: string
  Url: string
}

const props = withDefaults(defineProps<{
  content: string
  attachments?: MessageAttachment[]
  mentions?: MentionData
}>(), {
  attachments: () => [],
  mentions: () => ({ Users: {}, Roles: {}, Channels: {} }),
})

// Track which spoilers are revealed
const revealedSpoilers = ref<Set<number>>(new Set())

function toggleSpoiler(index: number) {
  if (revealedSpoilers.value.has(index)) {
    revealedSpoilers.value.delete(index)
  }
  else {
    revealedSpoilers.value.add(index)
  }
}

// Format Discord timestamp based on format type
function formatTimestamp(timestamp: number, format: string): string {
  const date = new Date(timestamp * 1000) // Unix timestamp is in seconds

  switch (format) {
    case 't': // Short time (e.g., "16:20")
      return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    case 'T': // Long time (e.g., "16:20:30")
      return date.toLocaleTimeString()
    case 'd': // Short date (e.g., "20/04/2021")
      return date.toLocaleDateString()
    case 'D': // Long date (e.g., "20 April 2021")
      return date.toLocaleDateString([], { day: 'numeric', month: 'long', year: 'numeric' })
    case 'f': // Short date/time (e.g., "20 April 2021 16:20")
      return `${date.toLocaleDateString([], { day: 'numeric', month: 'long', year: 'numeric' })} ${
        date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
    case 'F': // Long date/time (e.g., "Tuesday, 20 April 2021 16:20")
      return `${date.toLocaleDateString([], { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })} ${
        date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
    case 'R': // Relative time (e.g., "2 months ago")
      return getRelativeTime(date)
    default:
      return date.toLocaleString()
  }
}

// Get relative time string
function getRelativeTime(date: Date): string {
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffSec = Math.floor(diffMs / 1000)
  const diffMin = Math.floor(diffSec / 60)
  const diffHour = Math.floor(diffMin / 60)
  const diffDay = Math.floor(diffHour / 24)
  const diffMonth = Math.floor(diffDay / 30)
  const diffYear = Math.floor(diffDay / 365)

  if (diffSec < 60)
    return diffSec <= 1 ? 'just now' : `${diffSec} seconds ago`
  if (diffMin < 60)
    return diffMin === 1 ? '1 minute ago' : `${diffMin} minutes ago`
  if (diffHour < 24)
    return diffHour === 1 ? '1 hour ago' : `${diffHour} hours ago`
  if (diffDay < 30)
    return diffDay === 1 ? '1 day ago' : `${diffDay} days ago`
  if (diffMonth < 12)
    return diffMonth === 1 ? '1 month ago' : `${diffMonth} months ago`
  return diffYear === 1 ? '1 year ago' : `${diffYear} years ago`
}

// Parse Discord markdown and return an array of elements
const parsedContent = computed(() => {
  if (!props.content)
    return []

  const lines = props.content.split('\n')
  const elements: any[] = []
  let inCodeBlock = false
  let codeBlockContent: string[] = []
  let codeBlockLang = ''
  let listItems: any[] = []
  let listType: 'ul' | 'ol' | null = null
  let spoilerIndex = 0

  const flushList = () => {
    if (listItems.length > 0) {
      elements.push({ type: listType, items: [...listItems] })
      listItems = []
      listType = null
    }
  }

  const flushCodeBlock = () => {
    if (codeBlockContent.length > 0) {
      elements.push({
        type: 'codeblock',
        content: codeBlockContent.join('\n'),
        language: codeBlockLang,
      })
      codeBlockContent = []
      codeBlockLang = ''
    }
  }

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]

    // Handle code blocks
    if (line.startsWith('```')) {
      if (inCodeBlock) {
        flushCodeBlock()
        inCodeBlock = false
      }
      else {
        flushList()
        inCodeBlock = true
        codeBlockLang = line.substring(3).trim()
      }
      continue
    }

    if (inCodeBlock) {
      codeBlockContent.push(line)
      continue
    }

    // Handle headlines (## heading)
    const headlineMatch = line.match(/^##\s+(.+)$/)
    if (headlineMatch) {
      flushList()
      const parsed = parseInlineFormatting(headlineMatch[1], spoilerIndex)
      spoilerIndex += parsed.filter((t: any) => t.type === 'spoiler').length
      elements.push({ type: 'headline', content: parsed })
      continue
    }

    // Handle small text (-# text)
    const smallTextMatch = line.match(/^-#\s+(.+)$/)
    if (smallTextMatch) {
      flushList()
      const parsed = parseInlineFormatting(smallTextMatch[1], spoilerIndex)
      spoilerIndex += parsed.filter((t: any) => t.type === 'spoiler').length
      elements.push({ type: 'small', content: parsed })
      continue
    }

    // Handle unordered lists (- or *)
    const unorderedMatch = line.match(/^\s*[-*]\s+(.+)$/)
    if (unorderedMatch) {
      if (listType !== 'ul') {
        flushList()
        listType = 'ul'
      }
      const parsed = parseInlineFormatting(unorderedMatch[1], spoilerIndex)
      spoilerIndex += parsed.filter((t: any) => t.type === 'spoiler').length
      listItems.push(parsed)
      continue
    }

    // Handle ordered lists (1. 2. etc)
    const orderedMatch = line.match(/^\s*\d+\.\s+(.+)$/)
    if (orderedMatch) {
      if (listType !== 'ol') {
        flushList()
        listType = 'ol'
      }
      const parsed = parseInlineFormatting(orderedMatch[1], spoilerIndex)
      spoilerIndex += parsed.filter((t: any) => t.type === 'spoiler').length
      listItems.push(parsed)
      continue
    }

    // Not a list item, flush any pending list
    flushList()

    // Parse line with inline formatting
    const parsed = parseInlineFormatting(line, spoilerIndex)
    spoilerIndex += parsed.filter((t: any) => t.type === 'spoiler').length
    elements.push({ type: 'line', content: parsed })
  }

  // Flush any remaining code block or list
  flushCodeBlock()
  flushList()

  return elements
})

// Parse inline formatting (bold, italic, code, spoilers, etc.)
function parseInlineFormatting(text: string, startSpoilerIndex: number) {
  const tokens: any[] = []
  let currentText = ''
  let i = 0
  let spoilerCount = startSpoilerIndex

  const pushText = () => {
    if (currentText) {
      tokens.push({ type: 'text', content: currentText })
      currentText = ''
    }
  }

  while (i < text.length) {
    // Timestamp <t:timestamp:format>
    if (text[i] === '<' && text[i + 1] === 't' && text[i + 2] === ':') {
      pushText()
      const endIndex = text.indexOf('>', i)
      if (endIndex !== -1) {
        const timestampMatch = text.substring(i, endIndex + 1).match(/<t:(\d+)(?::([tTdDfFR]))?>/)
        if (timestampMatch) {
          const timestamp = Number.parseInt(timestampMatch[1])
          const format = timestampMatch[2] || 'f'
          tokens.push({ type: 'timestamp', timestamp, format })
          i = endIndex + 1
          continue
        }
      }
    }

    // Custom emotes <:name:id> or <a:name:id>
    if (text[i] === '<' && (text[i + 1] === ':' || (text[i + 1] === 'a' && text[i + 2] === ':'))) {
      const emoteMatch = text.substring(i).match(/^<(a?):(\w+):(\d+)>/)
      if (emoteMatch) {
        pushText()
        tokens.push({
          type: 'emote',
          name: emoteMatch[2],
          id: emoteMatch[3],
          animated: emoteMatch[1] === 'a',
        })
        i += emoteMatch[0].length
        continue
      }
    }

    // Role mentions <@&roleid>
    if (text[i] === '<' && text[i + 1] === '@' && text[i + 2] === '&') {
      const roleMatch = text.substring(i).match(/^<@&(\d+)>/)
      if (roleMatch) {
        pushText()
        tokens.push({
          type: 'roleMention',
          id: roleMatch[1],
          name: props.mentions.Roles[roleMatch[1]] ?? null,
        })
        i += roleMatch[0].length
        continue
      }
    }

    // User mentions <@userid> or <@!userid>
    if (text[i] === '<' && text[i + 1] === '@') {
      const userMatch = text.substring(i).match(/^<@!?(\d+)>/)
      if (userMatch) {
        pushText()
        tokens.push({
          type: 'userMention',
          id: userMatch[1],
          name: props.mentions.Users[userMatch[1]] ?? null,
        })
        i += userMatch[0].length
        continue
      }
    }

    // Channel mentions <#channelid>
    if (text[i] === '<' && text[i + 1] === '#') {
      const channelMatch = text.substring(i).match(/^<#(\d+)>/)
      if (channelMatch) {
        pushText()
        tokens.push({
          type: 'channelMention',
          id: channelMatch[1],
          name: props.mentions.Channels[channelMatch[1]] ?? null,
        })
        i += channelMatch[0].length
        continue
      }
    }

    // Masked links [text](url)
    if (text[i] === '[') {
      const maskedMatch = text.substring(i).match(/^\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)/)
      if (maskedMatch) {
        pushText()
        tokens.push({
          type: 'link',
          url: maskedMatch[2],
          display: maskedMatch[1],
        })
        i += maskedMatch[0].length
        continue
      }
    }

    // URLs (https:// or http://)
    if (text.substring(i, i + 8) === 'https://' || text.substring(i, i + 7) === 'http://') {
      const urlMatch = text.substring(i).match(/^https?:\/\/[^\s<>)]+/)
      if (urlMatch) {
        pushText()
        tokens.push({
          type: 'link',
          url: urlMatch[0],
          display: urlMatch[0],
        })
        i += urlMatch[0].length
        continue
      }
    }

    // Spoiler ||text||
    if (text.substr(i, 2) === '||') {
      pushText()
      const endIndex = text.indexOf('||', i + 2)
      if (endIndex !== -1) {
        const spoilerText = text.substring(i + 2, endIndex)
        tokens.push({ type: 'spoiler', content: spoilerText, index: spoilerCount++ })
        i = endIndex + 2
        continue
      }
    }

    // Code `text`
    if (text[i] === '`') {
      pushText()
      const endIndex = text.indexOf('`', i + 1)
      if (endIndex !== -1) {
        const codeText = text.substring(i + 1, endIndex)
        tokens.push({ type: 'code', content: codeText })
        i = endIndex + 1
        continue
      }
    }

    // Bold **text**
    if (text.substr(i, 2) === '**') {
      pushText()
      const endIndex = text.indexOf('**', i + 2)
      if (endIndex !== -1) {
        const boldText = text.substring(i + 2, endIndex)
        tokens.push({ type: 'bold', content: boldText })
        i = endIndex + 2
        continue
      }
    }

    // Italic *text* or _text_
    if (text[i] === '*' || text[i] === '_') {
      pushText()
      const char = text[i]
      const endIndex = text.indexOf(char, i + 1)
      if (endIndex !== -1 && text.substr(i, 2) !== '**') {
        const italicText = text.substring(i + 1, endIndex)
        tokens.push({ type: 'italic', content: italicText })
        i = endIndex + 1
        continue
      }
    }

    currentText += text[i]
    i++
  }

  pushText()
  return tokens
}
</script>

<script lang="ts">
function isImage(filename: string): boolean {
  const imageExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp', '.svg']
  return imageExtensions.some(ext => filename.toLowerCase().endsWith(ext))
}
</script>

<template>
  <div class="discord-message">
    <!-- Render parsed content -->
    <div class="discord-message__content">
      <template v-for="(element, idx) in parsedContent" :key="idx">
        <!-- Headline -->
        <h2 v-if="element.type === 'headline'" class="discord-headline">
          <TokenRenderer
            :format-timestamp="formatTimestamp" :revealed-spoilers="revealedSpoilers"
            :toggle-spoiler="toggleSpoiler" :tokens="element.content"
          />
        </h2>

        <!-- Small text -->
        <div v-else-if="element.type === 'small'" class="discord-small">
          <TokenRenderer
            :format-timestamp="formatTimestamp" :revealed-spoilers="revealedSpoilers"
            :toggle-spoiler="toggleSpoiler" :tokens="element.content"
          />
        </div>

        <!-- Line with inline formatting -->
        <div v-else-if="element.type === 'line'" class="discord-line">
          <TokenRenderer
            :format-timestamp="formatTimestamp" :revealed-spoilers="revealedSpoilers"
            :toggle-spoiler="toggleSpoiler" :tokens="element.content"
          />
        </div>

        <!-- Code block -->
        <pre v-else-if="element.type === 'codeblock'" class="discord-codeblock"><code>{{ element.content }}</code></pre>

        <!-- Unordered list -->
        <ul v-else-if="element.type === 'ul'" class="discord-list">
          <li v-for="(item, itemIdx) in element.items" :key="itemIdx">
            <TokenRenderer
              :format-timestamp="formatTimestamp" :revealed-spoilers="revealedSpoilers" :toggle-spoiler="toggleSpoiler"
              :tokens="item"
            />
          </li>
        </ul>

        <!-- Ordered list -->
        <ol v-else-if="element.type === 'ol'" class="discord-list">
          <li v-for="(item, itemIdx) in element.items" :key="itemIdx">
            <TokenRenderer
              :format-timestamp="formatTimestamp" :revealed-spoilers="revealedSpoilers" :toggle-spoiler="toggleSpoiler"
              :tokens="item"
            />
          </li>
        </ol>
      </template>
    </div>

    <!-- Render attachments (images) -->
    <div v-if="attachments && attachments.length > 0" class="discord-message__attachments">
      <div v-for="(attachment, idx) in attachments" :key="idx" class="discord-attachment">
        <img
          v-if="isImage(attachment.Name)"
          :src="attachment.Url"
          :alt="attachment.Name"
          class="discord-attachment__image"
        >
        <a
          v-else
          :href="attachment.Url"
          target="_blank"
          rel="noopener noreferrer"
          class="discord-attachment__link"
        >
          {{ attachment.Name }}
        </a>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ========================================
   Discord Message Renderer Styles
   ======================================== */

/* Main message container */
.discord-message {
  color: var(--fg);
  line-height: 1.6;
}

/* Message content with preserved whitespace */
.discord-message__content {
  white-space: pre-wrap;
  word-wrap: break-word;
  overflow-wrap: break-word;
}

/* Individual line */
.discord-line {
  margin: 0.25rem 0;
}

.discord-line:empty {
  min-height: 1.2em;
}

/* Inline code */
.discord-inline-code {
  background: var(--muted-bg);
  color: var(--fg);
  padding: 0.15rem 0.35rem;
  border-radius: 8px;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.9em;
  border: 1px solid var(--border);
}

/* Code blocks */
.discord-codeblock {
  background: var(--muted-bg);
  color: var(--fg);
  padding: 0.75rem;
  border-radius: 8px;
  border: 1px solid var(--border);
  overflow-x: auto;
  margin: 0.5rem 0;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.9em;
  line-height: 1.5;
}

.discord-codeblock code {
  background: none;
  padding: 0;
  border: none;
  color: inherit;
}

/* Lists */
.discord-list {
  margin: 0.5rem 0;
  padding-left: 1.5rem;
}

.discord-list li {
  margin: 0.25rem 0;
}

/* Spoilers */
.discord-spoiler {
  background: var(--muted);
  color: transparent;
  padding: 0 0.25rem;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
  user-select: none;
}

.discord-spoiler:hover {
  background: color-mix(in oklab, var(--muted) 80%, var(--fg) 20%);
}

.discord-spoiler.revealed {
  background: var(--muted-bg);
  color: var(--fg);
}

.discord-spoiler:focus {
  outline: 2px solid var(--ring);
  outline-offset: 2px;
}

/* Headlines */
.discord-headline {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--fg);
  margin: 0.75rem 0 0.5rem 0;
  line-height: 1.3;
}

/* Small text */
.discord-small {
  font-size: 0.8rem;
  color: var(--muted);
  margin: 0.25rem 0;
  line-height: 1.4;
}

/* Timestamps */
.discord-timestamp {
  background: var(--muted-bg);
  color: var(--fg);
  padding: 0.1rem 0.3rem;
  border-radius: 8px;
  font-size: 0.95em;
  white-space: nowrap;
}

/* Attachments container */
.discord-message__attachments {
  margin-top: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

/* Individual attachment */
.discord-attachment {
  display: inline-block;
}

/* Attachment images */
.discord-attachment__image {
  max-width: 100%;
  max-height: 400px;
  border-radius: 8px;
  border: 1px solid var(--border);
  display: block;
  object-fit: contain;
}

/* Attachment links (non-images) */
.discord-attachment__link {
  display: inline-flex;
  align-items: center;
  padding: 0.5rem 0.75rem;
  background: var(--muted-bg);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--link);
  text-decoration: none;
  font-size: 0.9rem;
  transition: background 0.2s ease;
}

.discord-attachment__link:hover {
  background: var(--card);
  text-decoration: underline;
}

/* Mentions */
.discord-mention {
  padding: 0 0.25rem;
  border-radius: 8px;
  font-weight: 500;
  white-space: nowrap;
}

.discord-mention--user,
.discord-mention--channel {
  background: rgba(59, 130, 246, 0.15);
  color: rgb(59, 130, 246);
}

.discord-mention--role {
  background: rgba(139, 92, 246, 0.15);
  color: rgb(139, 92, 246);
}

/* Custom emotes */
.discord-emote {
  display: inline;
  height: 1.375em;
  width: 1.375em;
  object-fit: contain;
  vertical-align: bottom;
}

/* Links */
.discord-link {
  color: var(--link);
  text-decoration: none;
}

.discord-link:hover {
  text-decoration: underline;
}

/* Dark theme adjustments */
:root[data-theme='dark'] .discord-inline-code,
:root[data-theme='dark'] .discord-codeblock {
  background: rgba(0, 0, 0, 0.3);
}

:root[data-theme='dark'] .discord-mention--user,
:root[data-theme='dark'] .discord-mention--channel {
  background: rgba(59, 130, 246, 0.2);
  color: rgb(96, 165, 250);
}

:root[data-theme='dark'] .discord-mention--role {
  background: rgba(139, 92, 246, 0.2);
  color: rgb(167, 139, 250);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .discord-inline-code,
  :root:not([data-theme='light']) .discord-codeblock {
    background: rgba(0, 0, 0, 0.3);
  }

  :root:not([data-theme='light']) .discord-mention--user,
  :root:not([data-theme='light']) .discord-mention--channel {
    background: rgba(59, 130, 246, 0.2);
    color: rgb(96, 165, 250);
  }

  :root:not([data-theme='light']) .discord-mention--role {
    background: rgba(139, 92, 246, 0.2);
    color: rgb(167, 139, 250);
  }
}

/* Respect prefers-reduced-motion for Discord elements */
@media (prefers-reduced-motion: reduce) {
  .discord-spoiler,
  .discord-attachment__link {
    transition: none !important;
  }
}
</style>
