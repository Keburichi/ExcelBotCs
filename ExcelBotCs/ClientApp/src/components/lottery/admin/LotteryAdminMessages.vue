<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { useLotteryAdmin } from '@/composables/useLotteryAdmin'
import LotteryAnnouncementPreview from './LotteryAnnouncementPreview.vue'

const admin = useLotteryAdmin()

const settings = admin.settings
const status = admin.status
const busy = admin.busy
const fieldErrors = admin.fieldErrors
const preview = admin.preview

// The placeholder contract of spec §5.6. Unknown placeholders are rejected by the server on save; the chips are the
// supported list and insert at the cursor so an admin never has to type one by hand.
const PLACEHOLDERS = ['{number}', '{winners}', '{prize}', '{bonusItems}', '{pot}', '{nextPot}', '{emoji}']

const draft = reactive({
  WinTemplate: '',
  NoWinTemplate: '',
})

const winEditor = ref<HTMLTextAreaElement | null>(null)
const noWinEditor = ref<HTMLTextAreaElement | null>(null)

const saved = computed(() => {
  const value = settings.value
  if (!value)
    return null

  return { WinTemplate: value.WinTemplate, NoWinTemplate: value.NoWinTemplate }
})

const dirty = computed(() => {
  const value = saved.value
  if (!value)
    return false

  return draft.WinTemplate !== value.WinTemplate || draft.NoWinTemplate !== value.NoWinTemplate
})

/** The visible count of the last rendered preview, so the editor shows the real budget, not just the template length. */
const previewBudget = computed(() => {
  if (!preview.value)
    return ''

  return `${preview.value.VisibleLength} / 2,000 visible characters rendered`
})

function fillFromSettings() {
  const value = settings.value
  if (!value)
    return

  draft.WinTemplate = value.WinTemplate
  draft.NoWinTemplate = value.NoWinTemplate
}

watch(settings, fillFromSettings, { immediate: true })

function insertPlaceholder(chip: string, editor: HTMLTextAreaElement | null, field: 'WinTemplate' | 'NoWinTemplate') {
  const start = editor?.selectionStart ?? draft[field].length
  const end = editor?.selectionEnd ?? start
  draft[field] = `${draft[field].slice(0, start)}${chip}${draft[field].slice(end)}`

  if (editor) {
    requestAnimationFrame(() => {
      editor.focus()
      const caret = start + chip.length
      editor.setSelectionRange(caret, caret)
    })
  }
}

function resetToDefaults() {
  const defaults = settings.value?.Defaults
  if (!defaults)
    return

  draft.WinTemplate = defaults.WinTemplate
  draft.NoWinTemplate = defaults.NoWinTemplate
}

async function save() {
  const value = settings.value
  if (!value)
    return

  await admin.saveSettings({
    RunDay: value.RunDay,
    RunTime: value.RunTime,
    RunTimezone: value.RunTimezone,
    ReminderDay: value.ReminderDay,
    ReminderTime: value.ReminderTime,
    ReminderTimezone: value.ReminderTimezone,
    RemindersEnabled: value.RemindersEnabled,
    PauseAutomation: value.PauseAutomation,
    WinTemplate: draft.WinTemplate,
    NoWinTemplate: draft.NoWinTemplate,
    ResetTemplates: false,
  })
}
</script>

<template>
  <div class="messages">
    <p v-if="!settings" class="empty">
      The settings have not loaded yet.
    </p>

    <template v-else>
      <section class="card" aria-label="Result templates">
        <h2 class="card__title">
          Result templates
        </h2>
        <p class="hint">
          Only the result block is editable. The greeting, the rules, the deadline, the current pot, the last five
          winners and the history link are generated. Click a placeholder to insert it at the cursor; unknown
          placeholders are rejected on save.
        </p>

        <div class="editor">
          <div class="editor__head">
            <label for="win-template">Winner template</label>
            <span class="editor__count">{{ draft.WinTemplate.length }} characters</span>
          </div>
          <textarea
            id="win-template"
            ref="winEditor"
            v-model="draft.WinTemplate"
            rows="5"
            spellcheck="false"
          />
          <span v-if="fieldErrors.WinTemplate" class="field__error">{{ fieldErrors.WinTemplate }}</span>
          <div class="chips" role="group" aria-label="Winner placeholders">
            <button
              v-for="chip in PLACEHOLDERS"
              :key="`win-${chip}`"
              class="chip"
              type="button"
              @click="insertPlaceholder(chip, winEditor, 'WinTemplate')"
            >
              {{ chip }}
            </button>
          </div>
        </div>

        <div class="editor">
          <div class="editor__head">
            <label for="nowin-template">No-winner template</label>
            <span class="editor__count">{{ draft.NoWinTemplate.length }} characters</span>
          </div>
          <textarea
            id="nowin-template"
            ref="noWinEditor"
            v-model="draft.NoWinTemplate"
            rows="4"
            spellcheck="false"
          />
          <span v-if="fieldErrors.NoWinTemplate" class="field__error">{{ fieldErrors.NoWinTemplate }}</span>
          <div class="chips" role="group" aria-label="No-winner placeholders">
            <button
              v-for="chip in PLACEHOLDERS"
              :key="`nowin-${chip}`"
              class="chip"
              type="button"
              @click="insertPlaceholder(chip, noWinEditor, 'NoWinTemplate')"
            >
              {{ chip }}
            </button>
          </div>
        </div>

        <div class="actions">
          <BaseButton
            state="primary"
            size="small"
            title="Save templates"
            :disabled="busy"
            @clicked="save"
          />
          <BaseButton
            state="tertiary"
            size="small"
            title="Reset to default"
            :disabled="busy"
            @clicked="resetToDefaults"
          />
          <BaseButton
            state="secondary"
            size="small"
            title="Preview winner"
            :disabled="busy"
            @clicked="admin.openPreview('win')"
          />
          <BaseButton
            state="secondary"
            size="small"
            title="Preview no-winner"
            :disabled="busy"
            @clicked="admin.openPreview('nowin')"
          />
        </div>
        <p v-if="dirty" class="dirty-note">
          Unsaved changes — Save is the only commit. Previews show the saved copy.
        </p>
        <p v-if="previewBudget" class="hint">
          Last preview: {{ previewBudget }}
        </p>
      </section>

      <section class="card" aria-label="Generated content">
        <h2 class="card__title">
          Generated content <span class="card__note">read-only</span>
        </h2>
        <ul class="generated">
          <li>The greeting and the lottery member role ping.</li>
          <li>The events and riddles channel pointers.</li>
          <li>The guessing rules and the submission deadline (from the Schedule tab).</li>
          <li>The current pot, including the prize-pool count.</li>
          <li>The last five winners, newest first, and the history link to the web page.</li>
        </ul>
        <p class="hint">
          <strong>Preview</strong> shows all of these assembled with the live pot and pool.
        </p>
      </section>

      <section class="card" aria-label="Deployment configuration">
        <h2 class="card__title">
          Deployment config <span class="card__note">read-only</span>
        </h2>
        <dl class="facts">
          <div class="fact">
            <dt>Lottery channel</dt>
            <dd class="mono">
              {{ status?.Deployment.LotteryChannelId ?? '—' }}
            </dd>
          </div>
          <div class="fact">
            <dt>Member role</dt>
            <dd class="mono">
              {{ status?.Deployment.LotteryMemberRoleId ?? '—' }}
            </dd>
          </div>
          <div class="fact">
            <dt>Emoji id</dt>
            <dd class="mono">
              {{ status?.Deployment.LotteryCustomEmojiId ?? '—' }}
            </dd>
          </div>
          <div class="fact">
            <dt>Web base URL</dt>
            <dd class="mono">
              {{ status?.Deployment.WebBaseUrl || '—' }}
            </dd>
          </div>
        </dl>
      </section>

      <LotteryAnnouncementPreview />
    </template>
  </div>
</template>

<style scoped>
.messages {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.card {
  padding: 1.25rem;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--card);
}

.card__title {
  margin: 0 0 0.75rem 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
}

.card__note {
  margin-left: 0.375rem;
  font-size: 0.7rem;
  font-weight: 500;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.editor {
  margin-bottom: 1.25rem;
}

.editor__head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.375rem;
}

.editor__head label {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--fg);
}

.editor__count {
  color: var(--muted);
  font-size: 0.72rem;
  font-variant-numeric: tabular-nums;
}

textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--border);
  border-radius: 10px;
  background: var(--bg);
  color: var(--fg);
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.82rem;
  line-height: 1.6;
  resize: vertical;
}

textarea:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.field__error {
  display: block;
  margin-top: 0.375rem;
  color: var(--msg-error-fg);
  font-size: 0.75rem;
}

.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  margin-top: 0.5rem;
}

.chip {
  padding: 0.2rem 0.5rem;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: var(--muted-bg);
  color: var(--muted);
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.72rem;
  cursor: pointer;
}

.chip:hover {
  color: var(--fg);
  border-color: color-mix(in oklab, var(--link) 40%, var(--border));
}

.chip:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.dirty-note {
  margin: 0.75rem 0 0 0;
  color: var(--muted);
  font-size: 0.78rem;
  font-style: italic;
}

.hint {
  margin: 0.5rem 0 0 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.5;
}

.generated {
  margin: 0.75rem 0 0 0;
  padding-left: 1.25rem;
  color: var(--muted);
  font-size: 0.82rem;
  line-height: 1.7;
}

.facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr));
  gap: 0.75rem 1.25rem;
  margin: 0;
}

.fact dt {
  margin-bottom: 0.125rem;
  color: var(--muted);
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.fact dd {
  margin: 0;
  color: var(--fg);
  font-size: 0.9rem;
}

.mono {
  font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
  font-size: 0.8rem;
  word-break: break-all;
}

.empty {
  margin: 0;
  color: var(--muted);
  font-size: 0.85rem;
}

/* Tablet: the editor head stacks so the character count does not crowd the label. */
@media (max-width: 768px) {
  .card {
    padding: 1rem;
  }

  .editor__head {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
  }
}

/* Phone: full-width actions and a slightly smaller monospace editor. */
@media (max-width: 480px) {
  .card {
    padding: 0.875rem;
  }

  .actions {
    flex-direction: column;
    align-items: stretch;
  }

  textarea {
    font-size: 0.78rem;
  }
}
</style>
