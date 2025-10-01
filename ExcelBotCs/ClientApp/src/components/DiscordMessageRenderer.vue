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
        <!-- Line with inline formatting -->
        <div v-if="element.type === 'line'" class="discord-line">
          <template v-for="(token, tokenIdx) in element.content" :key="tokenIdx">
            <span v-if="token.type === 'text'">{{ token.content }}</span>
            <strong v-else-if="token.type === 'bold'">{{ token.content }}</strong>
            <em v-else-if="token.type === 'italic'">{{ token.content }}</em>
            <code v-else-if="token.type === 'code'" class="discord-inline-code">{{ token.content }}</code>
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
