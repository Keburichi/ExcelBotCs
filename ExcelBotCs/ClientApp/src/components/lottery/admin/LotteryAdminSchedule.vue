<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { useLotteryAdmin } from '@/composables/useLotteryAdmin'

const admin = useLotteryAdmin()

const settings = admin.settings
const optIns = admin.optIns
const busy = admin.busy
const fieldErrors = admin.fieldErrors

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']

// The draft is the only commit path: edits stay here until Save, so a half-edited form never leaks into the
// scheduler (spec §5.9 "Save is the only commit").
const draft = reactive({
  RunDay: 'Tuesday',
  RunTime: '20:00',
  RunTimezone: 'Europe/Berlin',
  ReminderDay: 'Monday',
  ReminderTime: '18:00',
  ReminderTimezone: 'Europe/Berlin',
  RemindersEnabled: false,
})

const savedValues = computed(() => {
  const value = settings.value
  if (!value)
    return null

  return {
    RunDay: value.RunDay,
    RunTime: value.RunTime,
    RunTimezone: value.RunTimezone,
    ReminderDay: value.ReminderDay,
    ReminderTime: value.ReminderTime,
    ReminderTimezone: value.ReminderTimezone,
    RemindersEnabled: value.RemindersEnabled,
  }
})

const dirty = computed(() => {
  const saved = savedValues.value
  if (!saved)
    return false

  return (Object.keys(draft) as Array<keyof typeof draft>).some(key => draft[key] !== saved[key])
})

const optInsLoading = ref(false)

function fillFromSettings() {
  const value = settings.value
  if (!value)
    return

  draft.RunDay = value.RunDay
  draft.RunTime = value.RunTime
  draft.RunTimezone = value.RunTimezone
  draft.ReminderDay = value.ReminderDay
  draft.ReminderTime = value.ReminderTime
  draft.ReminderTimezone = value.ReminderTimezone
  draft.RemindersEnabled = value.RemindersEnabled
}

watch(settings, fillFromSettings, { immediate: true })

onMounted(() => {
  if (!settings.value && !admin.loading.value)
    void admin.loadAll()
})

async function save() {
  const value = settings.value
  if (!value)
    return

  // The templates and the pause flag are not on this form; sending the stored values keeps the save a full-set save
  // without silently resetting the other tabs.
  await admin.saveSettings({
    RunDay: draft.RunDay,
    RunTime: draft.RunTime,
    RunTimezone: draft.RunTimezone,
    ReminderDay: draft.ReminderDay,
    ReminderTime: draft.ReminderTime,
    ReminderTimezone: draft.ReminderTimezone,
    RemindersEnabled: draft.RemindersEnabled,
    PauseAutomation: value.PauseAutomation,
    WinTemplate: value.WinTemplate,
    NoWinTemplate: value.NoWinTemplate,
    ResetTemplates: false,
  })
}

async function sendTest() {
  await admin.sendTestReminder()
}

async function removeOptIn(discordId: string) {
  const name = optIns.value?.Items.find(item => item.DiscordId === discordId)?.DisplayName ?? discordId
  if (!window.confirm(`Remove ${name} from the lottery reminder list? They can opt back in from the lottery page.`))
    return

  optInsLoading.value = true
  try {
    await admin.removeOptIn(discordId)
  }
  finally {
    optInsLoading.value = false
  }
}
</script>

<template>
  <div class="schedule">
    <p v-if="!settings" class="empty">
      The settings have not loaded yet.
    </p>

    <template v-else>
      <section class="card" aria-label="Schedule">
        <h2 class="card__title">
          Schedule
        </h2>
        <p class="hint">
          All times are wall-clock times in the configured IANA timezone, so DST transitions are handled for you.
        </p>

        <div class="grid">
          <div class="field">
            <label for="run-day">Run day</label>
            <select id="run-day" v-model="draft.RunDay">
              <option v-for="day in DAYS" :key="day" :value="day">
                {{ day }}
              </option>
            </select>
            <span v-if="fieldErrors.RunDay" class="field__error">{{ fieldErrors.RunDay }}</span>
          </div>

          <div class="field">
            <label for="run-time">Run time</label>
            <input id="run-time" v-model="draft.RunTime" type="time" step="60">
            <span v-if="fieldErrors.RunTime" class="field__error">{{ fieldErrors.RunTime }}</span>
          </div>

          <div class="field">
            <label for="run-timezone">Run timezone</label>
            <input id="run-timezone" v-model="draft.RunTimezone" type="text" placeholder="Europe/Berlin">
            <span v-if="fieldErrors.RunTimezone" class="field__error">{{ fieldErrors.RunTimezone }}</span>
          </div>
        </div>
      </section>

      <section class="card" aria-label="Reminders">
        <h2 class="card__title">
          Reminders
        </h2>

        <div class="grid">
          <div class="field">
            <label for="reminder-day">Reminder day</label>
            <select id="reminder-day" v-model="draft.ReminderDay">
              <option v-for="day in DAYS" :key="day" :value="day">
                {{ day }}
              </option>
            </select>
            <span v-if="fieldErrors.ReminderDay" class="field__error">{{ fieldErrors.ReminderDay }}</span>
          </div>

          <div class="field">
            <label for="reminder-time">Reminder time</label>
            <input id="reminder-time" v-model="draft.ReminderTime" type="time" step="60">
            <span v-if="fieldErrors.ReminderTime" class="field__error">{{ fieldErrors.ReminderTime }}</span>
          </div>

          <div class="field">
            <label for="reminder-timezone">Reminder timezone</label>
            <input id="reminder-timezone" v-model="draft.ReminderTimezone" type="text" placeholder="Europe/Berlin">
            <span v-if="fieldErrors.ReminderTimezone" class="field__error">{{ fieldErrors.ReminderTimezone }}</span>
          </div>
        </div>

        <label class="toggle">
          <input v-model="draft.RemindersEnabled" type="checkbox">
          <span>Send weekly reminder</span>
        </label>
        <span v-if="fieldErrors.RemindersEnabled" class="field__error">{{ fieldErrors.RemindersEnabled }}</span>
        <p class="hint">
          The reminder pings only opted-in members with an unused guess, once per week, before the draw. The deployment
          master switch can still disable it entirely.
        </p>

        <div class="save-row">
          <BaseButton
            state="primary"
            size="small"
            title="Save settings"
            :disabled="busy"
            @clicked="save"
          />
          <span v-if="dirty" class="dirty-note">Unsaved changes — Save is the only commit.</span>
        </div>
      </section>

      <section class="card" aria-label="Opted-in members">
        <div class="card__head">
          <h2 class="card__title">
            Opted-in members
          </h2>
          <BaseButton
            state="secondary"
            size="small"
            title="Send test reminder"
            :disabled="busy"
            @clicked="sendTest"
          />
        </div>

        <p class="hint">
          {{ optIns?.OptedIn ?? 0 }} opted in · {{ optIns?.Eligible ?? 0 }} would be pinged right now. The test reminder
          goes to you only, never to the channel.
        </p>

        <p v-if="!optIns || optIns.Items.length === 0" class="empty">
          Nobody has reminders turned on yet.
        </p>

        <ul v-else class="optins">
          <li v-for="item in optIns.Items" :key="item.DiscordId" class="optin">
            <div class="optin__who">
              <span class="optin__name">{{ item.DisplayName ?? item.DiscordId }}</span>
              <span class="optin__meta">
                <span v-if="!item.IsCurrentMember" class="tag tag--muted">not a current member</span>
                <span v-else-if="!item.IsEligible" class="tag tag--muted">no unused guess</span>
                <span v-else class="tag tag--ok">will be pinged</span>
              </span>
            </div>
            <BaseButton
              state="tertiary"
              size="small"
              :disabled="busy || optInsLoading"
              title="Remove"
              @clicked="removeOptIn(item.DiscordId)"
            />
          </li>
        </ul>
      </section>
    </template>
  </div>
</template>

<style scoped>
.schedule {
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

.card__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.card__title {
  margin: 0 0 0.75rem 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
}

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: 1rem;
  margin-bottom: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.field label {
  font-size: 0.8rem;
  font-weight: 500;
  color: var(--fg);
}

.field input,
.field select {
  padding: 0.5rem 0.625rem;
  border: 1px solid var(--border);
  border-radius: 9px;
  background: var(--bg);
  color: var(--fg);
  font-size: 0.85rem;
}

.field input:focus-visible,
.field select:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.field__error {
  margin-top: 0.25rem;
  color: var(--msg-error-fg);
  font-size: 0.75rem;
}

.toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.85rem;
  color: var(--fg);
  cursor: pointer;
}

.toggle input {
  width: 1rem;
  height: 1rem;
  accent-color: var(--link);
}

.hint {
  margin: 0.5rem 0 0 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.5;
}

.save-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
  margin-top: 1rem;
}

.dirty-note {
  color: var(--muted);
  font-size: 0.78rem;
  font-style: italic;
}

.optins {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  margin: 0.75rem 0 0 0;
  padding: 0;
  list-style: none;
}

.optin {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--border);
  border-radius: 10px;
}

.optin__who {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
}

.optin__name {
  color: var(--fg);
  font-size: 0.85rem;
  font-weight: 500;
}

.optin__meta {
  display: inline-flex;
  gap: 0.375rem;
}

.tag {
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
  font-size: 0.68rem;
  font-weight: 600;
}

.tag--ok {
  background: var(--msg-success-bg);
  color: var(--msg-success-fg);
  border: 1px solid var(--msg-success-border);
}

.tag--muted {
  background: var(--muted-bg);
  color: var(--muted);
  border: 1px solid var(--border);
}

.empty {
  margin: 0.75rem 0 0 0;
  color: var(--muted);
  font-size: 0.85rem;
}
</style>
