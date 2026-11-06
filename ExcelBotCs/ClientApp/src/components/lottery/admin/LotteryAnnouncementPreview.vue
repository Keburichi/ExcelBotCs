<script setup lang="ts">
import { computed } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import { useLotteryAdmin } from '@/composables/useLotteryAdmin'

// The assembled announcement exactly as the bot would post it, for either result block (spec §5.6, §5.9). It is a
// preview of the *saved* templates with the live pot and pool, so the character budget shown here is the real one.
const { preview, previewOpen, previewVariant, busy, openPreview } = useLotteryAdmin()

const open = computed({
  get: () => previewOpen.value,
  set: (value: boolean) => {
    previewOpen.value = value
  },
})

const variantLabel = computed(() => (previewVariant.value === 'win' ? 'Winner' : 'No winner'))

const lengthLabel = computed(() => {
  if (!preview.value)
    return ''

  return `${preview.value.VisibleLength} visible characters · ${preview.value.RawLength} with Discord markup`
})

const budgetNote = computed(() => {
  if (!preview.value)
    return ''

  return preview.value.VisibleLength >= 2_000
    ? 'This post is at or over the 2,000-character limit; the run falls back to the default copy.'
    : `Room left: ${2_000 - preview.value.VisibleLength} characters.`
})
</script>

<template>
  <BaseModal
    v-model="open"
    :title="`Announcement preview — ${variantLabel}`"
    size="large"
  >
    <div class="preview">
      <div class="preview__bar">
        <div class="preview__variants" role="group" aria-label="Result variant">
          <BaseButton
            :state="previewVariant === 'win' ? 'primary' : 'secondary'"
            size="small"
            title="Winner"
            @clicked="openPreview('win')"
          />
          <BaseButton
            :state="previewVariant === 'nowin' ? 'primary' : 'secondary'"
            size="small"
            title="No winner"
            @clicked="openPreview('nowin')"
          />
        </div>
        <span v-if="preview" class="preview__count">{{ lengthLabel }}</span>
      </div>

      <p v-if="busy && !preview" class="preview__status" role="status">
        Rendering the announcement…
      </p>

      <pre v-if="preview" class="preview__text" tabindex="0">{{ preview.Text }}</pre>

      <p v-if="preview" class="preview__budget" :class="{ 'preview__budget--over': preview.VisibleLength >= 2000 }">
        {{ budgetNote }}
      </p>

      <p class="preview__hint">
        The generated sections — the greeting, the rules, the deadline, the current pot, the last five winners and the
        history link — are not editable. Only the result block on the Messages tab is.
      </p>
    </div>
  </BaseModal>
</template>

<style scoped>
.preview {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.preview__bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.preview__variants {
  display: flex;
  gap: 0.375rem;
}

.preview__count {
  color: var(--muted);
  font-size: 0.78rem;
  font-variant-numeric: tabular-nums;
}

.preview__text {
  margin: 0;
  padding: 1rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--muted-bg);
  color: var(--fg);
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.8rem;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
  max-height: 55vh;
  overflow: auto;
}

.preview__text:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.preview__budget {
  margin: 0;
  color: var(--muted);
  font-size: 0.78rem;
}

.preview__budget--over {
  color: var(--msg-error-fg);
  font-weight: 600;
}

.preview__status {
  margin: 0;
  color: var(--muted);
  font-size: 0.85rem;
}

.preview__hint {
  margin: 0;
  color: var(--muted);
  font-size: 0.78rem;
  line-height: 1.5;
}

/* Tablet: the variant bar stacks above the preview instead of squeezing it. */
@media (max-width: 768px) {
  .preview__bar {
    flex-direction: column;
    align-items: flex-start;
  }

  .preview__text {
    padding: 0.75rem;
  }
}

/* Phone: the pre-wrap post wraps at the viewport, so the type shrinks one step. */
@media (max-width: 480px) {
  .preview__text {
    font-size: 0.78rem;
  }
}
</style>
