<script setup lang="ts">
import { computed } from 'vue';
import { ref } from 'vue';

interface MessageAttachment {
  Name: string;
  Url: string;
}

const props = withDefaults(defineProps<{
  content: string;
  attachments?: MessageAttachment[];
}>(), {
  attachments: () => []
});

// Track which spoilers are revealed
const revealedSpoilers = ref<Set<number>>(new Set());

function toggleSpoiler(index: number) {
  if (revealedSpoilers.value.has(index)) {
    revealedSpoilers.value.delete(index);
  } else {
    revealedSpoilers.value.add(index);
  }
}

// Format Discord timestamp based on format type
function formatTimestamp(timestamp: number, format: string): string {
  const date = new Date(timestamp * 1000); // Unix timestamp is in seconds
  
  switch (format) {
    case 't': // Short time (e.g., "16:20")
      return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    case 'T': // Long time (e.g., "16:20:30")
      return date.toLocaleTimeString();
    case 'd': // Short date (e.g., "20/04/2021")
      return date.toLocaleDateString();
    case 'D': // Long date (e.g., "20 April 2021")
      return date.toLocaleDateString([], { day: 'numeric', month: 'long', year: 'numeric' });
    case 'f': // Short date/time (e.g., "20 April 2021 16:20")
      return date.toLocaleDateString([], { day: 'numeric', month: 'long', year: 'numeric' }) + ' ' + 
             date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    case 'F': // Long date/time (e.g., "Tuesday, 20 April 2021 16:20")
      return date.toLocaleDateString([], { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }) + ' ' + 
             date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    case 'R': // Relative time (e.g., "2 months ago")
      return getRelativeTime(date);
    default:
      return date.toLocaleString();
  }
}

// Get relative time string
function getRelativeTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);
  const diffMin = Math.floor(diffSec / 60);
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);
  const diffMonth = Math.floor(diffDay / 30);
  const diffYear = Math.floor(diffDay / 365);
  
  if (diffSec < 60) return diffSec <= 1 ? 'just now' : `${diffSec} seconds ago`;
  if (diffMin < 60) return diffMin === 1 ? '1 minute ago' : `${diffMin} minutes ago`;
  if (diffHour < 24) return diffHour === 1 ? '1 hour ago' : `${diffHour} hours ago`;
  if (diffDay < 30) return diffDay === 1 ? '1 day ago' : `${diffDay} days ago`;
  if (diffMonth < 12) return diffMonth === 1 ? '1 month ago' : `${diffMonth} months ago`;
  return diffYear === 1 ? '1 year ago' : `${diffYear} years ago`;
}

// Parse Discord markdown and return an array of elements
const parsedContent = computed(() => {
  if (!props.content) return [];
  
  const lines = props.content.split('\n');
  const elements: any[] = [];
  let inCodeBlock = false;
  let codeBlockContent: string[] = [];
  let codeBlockLang = '';
  let listItems: any[] = [];
  let listType: 'ul' | 'ol' | null = null;
  let spoilerIndex = 0;
  
  const flushList = () => {
    if (listItems.length > 0) {
      elements.push({ type: listType, items: [...listItems] });
      listItems = [];
      listType = null;
    }
  };
  
  const flushCodeBlock = () => {
    if (codeBlockContent.length > 0) {
      elements.push({ 
        type: 'codeblock', 
        content: codeBlockContent.join('\n'),
        language: codeBlockLang
      });
      codeBlockContent = [];
      codeBlockLang = '';
    }
  };
  
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    
    // Handle code blocks
    if (line.startsWith('```')) {
      if (inCodeBlock) {
        flushCodeBlock();
        inCodeBlock = false;
      } else {
        flushList();
        inCodeBlock = true;
        codeBlockLang = line.substring(3).trim();
      }
      continue;
    }
    
    if (inCodeBlock) {
      codeBlockContent.push(line);
      continue;
    }
    
    // Handle headlines (## heading)
    const headlineMatch = line.match(/^##\s+(.+)$/);
    if (headlineMatch) {
      flushList();
      const parsed = parseInlineFormatting(headlineMatch[1], spoilerIndex);
      elements.push({ type: 'headline', content: parsed });
      continue;
    }
    
    // Handle small text (-# text)
    const smallTextMatch = line.match(/^-#\s+(.+)$/);
    if (smallTextMatch) {
      flushList();
      const parsed = parseInlineFormatting(smallTextMatch[1], spoilerIndex);
      elements.push({ type: 'small', content: parsed });
      continue;
    }
    
    // Handle unordered lists (- or *)
    const unorderedMatch = line.match(/^[\s]*[-*]\s+(.+)$/);
    if (unorderedMatch) {
      if (listType !== 'ul') {
        flushList();
        listType = 'ul';
      }
      listItems.push(parseInlineFormatting(unorderedMatch[1], spoilerIndex));
      continue;
    }
    
    // Handle ordered lists (1. 2. etc)
    const orderedMatch = line.match(/^[\s]*\d+\.\s+(.+)$/);
    if (orderedMatch) {
      if (listType !== 'ol') {
        flushList();
        listType = 'ol';
      }
      listItems.push(parseInlineFormatting(orderedMatch[1], spoilerIndex));
      continue;
    }
    
    // Not a list item, flush any pending list
    flushList();
    
    // Parse line with inline formatting
    const parsed = parseInlineFormatting(line, spoilerIndex);
    elements.push({ type: 'line', content: parsed });
  }
  
  // Flush any remaining code block or list
  flushCodeBlock();
  flushList();
  
  return elements;
});

// Parse inline formatting (bold, italic, code, spoilers, etc.)
function parseInlineFormatting(text: string, startSpoilerIndex: number) {
  const tokens: any[] = [];
  let currentText = '';
  let i = 0;
  let spoilerCount = startSpoilerIndex;
  
  const pushText = () => {
    if (currentText) {
      tokens.push({ type: 'text', content: currentText });
      currentText = '';
    }
  };
  
  while (i < text.length) {
    // Timestamp <t:timestamp:format>
    if (text[i] === '<' && text[i + 1] === 't' && text[i + 2] === ':') {
      pushText();
      const endIndex = text.indexOf('>', i);
      if (endIndex !== -1) {
        const timestampMatch = text.substring(i, endIndex + 1).match(/<t:(\d+)(?::([tTdDfFR]))?>/);
        if (timestampMatch) {
          const timestamp = parseInt(timestampMatch[1]);
          const format = timestampMatch[2] || 'f';
          tokens.push({ type: 'timestamp', timestamp, format });
          i = endIndex + 1;
          continue;
        }
      }
    }
    
    // Spoiler ||text||
    if (text.substr(i, 2) === '||') {
      pushText();
      const endIndex = text.indexOf('||', i + 2);
      if (endIndex !== -1) {
        const spoilerText = text.substring(i + 2, endIndex);
        tokens.push({ type: 'spoiler', content: spoilerText, index: spoilerCount++ });
        i = endIndex + 2;
        continue;
      }
    }
    
    // Code `text`
    if (text[i] === '`') {
      pushText();
      const endIndex = text.indexOf('`', i + 1);
      if (endIndex !== -1) {
        const codeText = text.substring(i + 1, endIndex);
        tokens.push({ type: 'code', content: codeText });
        i = endIndex + 1;
        continue;
      }
    }
    
    // Bold **text**
    if (text.substr(i, 2) === '**') {
      pushText();
      const endIndex = text.indexOf('**', i + 2);
      if (endIndex !== -1) {
        const boldText = text.substring(i + 2, endIndex);
        tokens.push({ type: 'bold', content: boldText });
        i = endIndex + 2;
        continue;
      }
    }
    
    // Italic *text* or _text_
    if (text[i] === '*' || text[i] === '_') {
      pushText();
      const char = text[i];
      const endIndex = text.indexOf(char, i + 1);
      if (endIndex !== -1 && text.substr(i, 2) !== '**') {
        const italicText = text.substring(i + 1, endIndex);
        tokens.push({ type: 'italic', content: italicText });
        i = endIndex + 1;
        continue;
      }
    }
    
    currentText += text[i];
    i++;
  }
  
  pushText();
  return tokens;
}
</script>

<template>
  <div class="discord-message">
    <!-- Render parsed content -->
    <div class="discord-message__content">
      <template v-for="(element, idx) in parsedContent" :key="idx">
        <!-- Headline -->
        <h2 v-if="element.type === 'headline'" class="discord-headline">
          <template v-for="(token, tokenIdx) in element.content" :key="tokenIdx">
            <span v-if="token.type === 'text'">{{ token.content }}</span>
            <strong v-else-if="token.type === 'bold'">{{ token.content }}</strong>
            <em v-else-if="token.type === 'italic'">{{ token.content }}</em>
            <code v-else-if="token.type === 'code'" class="discord-inline-code">{{ token.content }}</code>
            <span v-else-if="token.type === 'timestamp'" class="discord-timestamp">{{ formatTimestamp(token.timestamp, token.format) }}</span>
            <span 
              v-else-if="token.type === 'spoiler'" 
              class="discord-spoiler"
              :class="{ revealed: revealedSpoilers.has(token.index) }"
              @click="toggleSpoiler(token.index)"
              role="button"
              tabindex="0"
              @keydown.enter.space.prevent="toggleSpoiler(token.index)"
            >
              {{ token.content }}
            </span>
          </template>
        </h2>
        
        <!-- Small text -->
        <div v-else-if="element.type === 'small'" class="discord-small">
          <template v-for="(token, tokenIdx) in element.content" :key="tokenIdx">
            <span v-if="token.type === 'text'">{{ token.content }}</span>
            <strong v-else-if="token.type === 'bold'">{{ token.content }}</strong>
            <em v-else-if="token.type === 'italic'">{{ token.content }}</em>
            <code v-else-if="token.type === 'code'" class="discord-inline-code">{{ token.content }}</code>
            <span v-else-if="token.type === 'timestamp'" class="discord-timestamp">{{ formatTimestamp(token.timestamp, token.format) }}</span>
            <span 
              v-else-if="token.type === 'spoiler'" 
              class="discord-spoiler"
              :class="{ revealed: revealedSpoilers.has(token.index) }"
              @click="toggleSpoiler(token.index)"
              role="button"
              tabindex="0"
              @keydown.enter.space.prevent="toggleSpoiler(token.index)"
            >
              {{ token.content }}
            </span>
          </template>
        </div>
        
        <!-- Line with inline formatting -->
        <div v-else-if="element.type === 'line'" class="discord-line">
          <template v-for="(token, tokenIdx) in element.content" :key="tokenIdx">
            <span v-if="token.type === 'text'">{{ token.content }}</span>
            <strong v-else-if="token.type === 'bold'">{{ token.content }}</strong>
            <em v-else-if="token.type === 'italic'">{{ token.content }}</em>
            <code v-else-if="token.type === 'code'" class="discord-inline-code">{{ token.content }}</code>
            <span v-else-if="token.type === 'timestamp'" class="discord-timestamp">{{ formatTimestamp(token.timestamp, token.format) }}</span>
            <span 
              v-else-if="token.type === 'spoiler'" 
              class="discord-spoiler"
              :class="{ revealed: revealedSpoilers.has(token.index) }"
              @click="toggleSpoiler(token.index)"
              role="button"
              tabindex="0"
              @keydown.enter.space.prevent="toggleSpoiler(token.index)"
            >
              {{ token.content }}
            </span>
          </template>
        </div>
        
        <!-- Code block -->
        <pre v-else-if="element.type === 'codeblock'" class="discord-codeblock"><code>{{ element.content }}</code></pre>
        
        <!-- Unordered list -->
        <ul v-else-if="element.type === 'ul'" class="discord-list">
          <li v-for="(item, itemIdx) in element.items" :key="itemIdx">
            <template v-for="(token, tokenIdx) in item" :key="tokenIdx">
              <span v-if="token.type === 'text'">{{ token.content }}</span>
              <strong v-else-if="token.type === 'bold'">{{ token.content }}</strong>
              <em v-else-if="token.type === 'italic'">{{ token.content }}</em>
              <code v-else-if="token.type === 'code'" class="discord-inline-code">{{ token.content }}</code>
              <span v-else-if="token.type === 'timestamp'" class="discord-timestamp">{{ formatTimestamp(token.timestamp, token.format) }}</span>
              <span 
                v-else-if="token.type === 'spoiler'" 
                class="discord-spoiler"
                :class="{ revealed: revealedSpoilers.has(token.index) }"
                @click="toggleSpoiler(token.index)"
                role="button"
                tabindex="0"
                @keydown.enter.space.prevent="toggleSpoiler(token.index)"
              >
                {{ token.content }}
              </span>
            </template>
          </li>
        </ul>
        
        <!-- Ordered list -->
        <ol v-else-if="element.type === 'ol'" class="discord-list">
          <li v-for="(item, itemIdx) in element.items" :key="itemIdx">
            <template v-for="(token, tokenIdx) in item" :key="tokenIdx">
              <span v-if="token.type === 'text'">{{ token.content }}</span>
              <strong v-else-if="token.type === 'bold'">{{ token.content }}</strong>
              <em v-else-if="token.type === 'italic'">{{ token.content }}</em>
              <code v-else-if="token.type === 'code'" class="discord-inline-code">{{ token.content }}</code>
              <span v-else-if="token.type === 'timestamp'" class="discord-timestamp">{{ formatTimestamp(token.timestamp, token.format) }}</span>
              <span 
                v-else-if="token.type === 'spoiler'" 
                class="discord-spoiler"
                :class="{ revealed: revealedSpoilers.has(token.index) }"
                @click="toggleSpoiler(token.index)"
                role="button"
                tabindex="0"
                @keydown.enter.space.prevent="toggleSpoiler(token.index)"
              >
                {{ token.content }}
              </span>
            </template>
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
        />
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

<script lang="ts">
function isImage(filename: string): boolean {
  const imageExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp', '.svg'];
  return imageExtensions.some(ext => filename.toLowerCase().endsWith(ext));
}
</script>

<style scoped>
/* Styles are intentionally empty - all styles are in main.css for theme consistency */
</style>
