<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { LotteryApi } from '@/features/lottery/lottery.api'

// The award-guess flow, moved here unchanged from the old in-page admin block (spec §5.9 "Guesses"). Officers keep
// the slash command; this is the web equivalent.
const loading = ref(false)
const error = ref('')
const success = ref('')
const reason = ref('')
const userInput = ref('')
const selectedUsers = ref<string[]>([])
const fcMembers = ref<string[]>([])
const showSuggestions = ref(false)

const filteredMembers = computed(() => {
  if (!userInput.value.trim())
    return []

  const search = userInput.value.toLowerCase()
  return fcMembers.value
    .filter(name => name.toLowerCase().includes(search) && !selectedUsers.value.includes(name))
    .slice(0, 10)
})

onMounted(async () => {
  try {
    fcMembers.value = await LotteryApi.fcMembers()
  }
  catch {
    // The autocomplete is a convenience; a failure here must not block awarding by exact name.
    fcMembers.value = []
  }
})

function addUser(userName: string) {
  if (!selectedUsers.value.includes(userName))
    selectedUsers.value.push(userName)

  userInput.value = ''
  showSuggestions.value = false
}

function removeUser(userName: string) {
  selectedUsers.value = selectedUsers.value.filter(user => user !== userName)
}

function handleInputBlur() {
  // A short delay lets the click on a suggestion land before the list disappears.
  setTimeout(() => (showSuggestions.value = false), 200)
}

async function award() {
  error.value = ''
  success.value = ''

  if (!reason.value.trim()) {
    error.value = 'Please enter a reason for awarding guesses.'
    return
  }

  if (selectedUsers.value.length === 0) {
    error.value = 'Please select at least one member.'
    return
  }

  loading.value = true
  try {
    const names = [...selectedUsers.value]
    const result = await LotteryApi.awardUsers(reason.value.trim(), names)
    success.value = result.message
    reason.value = ''
    selectedUsers.value = []
  }
  catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Failed to award guesses.'
  }
  finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="card" aria-label="Award guesses">
    <h2 class="card__title">
      Award guesses
    </h2>
    <p class="hint">
      Give additional guesses to specific members, for example for an event. The reason is stored on the award.
    </p>

    <div v-if="error" class="message message--error" role="alert">
      {{ error }}
    </div>
    <div v-if="success" class="message message--success" role="status">
      {{ success }}
    </div>

    <div class="field">
      <label for="award-reason">Reason</label>
      <input
        id="award-reason"
        v-model="reason"
        placeholder="e.g. Event participation reward"
        type="text"
      >
    </div>

    <div class="field autocomplete">
      <label for="award-users">Members</label>
      <input
        id="award-users"
        v-model="userInput"
        autocomplete="off"
        placeholder="Start typing a name…"
        type="text"
        @blur="handleInputBlur"
        @focus="showSuggestions = true"
      >
      <ul v-if="showSuggestions && filteredMembers.length > 0" class="suggestions" role="listbox">
        <li v-for="member in filteredMembers" :key="member" class="suggestion">
          <button type="button" @click="addUser(member)">
            {{ member }}
          </button>
        </li>
      </ul>
    </div>

    <ul v-if="selectedUsers.length > 0" class="chips" aria-label="Selected members">
      <li v-for="user in selectedUsers" :key="user" class="user-chip">
        <span>{{ user }}</span>
        <button :aria-label="`Remove ${user}`" class="chip-remove" type="button" @click="removeUser(user)">
          ×
        </button>
      </li>
    </ul>

    <div class="actions">
      <BaseButton
        state="tertiary"
        size="small"
        title="Award guesses"
        :disabled="loading"
        @clicked="award"
      />
    </div>
  </section>
</template>

<style scoped>
.card {
  padding: 1.25rem;
  border: 1px solid var(--border);
  border-radius: 14px;
  background: var(--card);
  max-width: 40rem;
}

.card__title {
  margin: 0 0 0.5rem 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
}

.hint {
  margin: 0 0 1rem 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.5;
}

.field {
  margin-bottom: 0.875rem;
}

.field label {
  display: block;
  margin-bottom: 0.375rem;
  font-size: 0.8rem;
  font-weight: 500;
  color: var(--fg);
}

.field input {
  width: 100%;
  padding: 0.5rem 0.625rem;
  border: 1px solid var(--border);
  border-radius: 9px;
  background: var(--bg);
  color: var(--fg);
  font-size: 0.85rem;
}

.field input:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.autocomplete {
  position: relative;
}

.suggestions {
  position: absolute;
  z-index: 10;
  top: 100%;
  left: 0;
  right: 0;
  margin: 0.25rem 0 0 0;
  padding: 0.25rem;
  max-height: 12rem;
  overflow-y: auto;
  list-style: none;
  border: 1px solid var(--border);
  border-radius: 10px;
  background: var(--card);
  box-shadow: var(--elev);
}

.suggestion button {
  display: block;
  width: 100%;
  padding: 0.45rem 0.625rem;
  border: none;
  border-radius: 7px;
  background: transparent;
  color: var(--fg);
  font-size: 0.85rem;
  text-align: left;
  cursor: pointer;
}

.suggestion button:hover {
  background: color-mix(in oklab, var(--link) 8%, transparent);
}

.suggestion button:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  margin: 0 0 1rem 0;
  padding: 0;
  list-style: none;
}

.user-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.25rem 0.625rem;
  border-radius: 999px;
  background: var(--link);
  color: var(--bg);
  font-size: 0.8rem;
}

.chip-remove {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 1rem;
  height: 1rem;
  padding: 0;
  border: none;
  border-radius: 50%;
  background: none;
  color: var(--bg);
  font-size: 1rem;
  line-height: 1;
  cursor: pointer;
}

.chip-remove:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.actions {
  display: flex;
  gap: 0.5rem;
}

.message {
  padding: 0.75rem 1rem;
  border-radius: 12px;
  margin-bottom: 0.75rem;
  font-size: 0.85rem;
}

.message--error {
  background: var(--msg-error-bg);
  color: var(--msg-error-fg);
  border: 1px solid var(--msg-error-border);
}

.message--success {
  background: var(--msg-success-bg);
  color: var(--msg-success-fg);
  border: 1px solid var(--msg-success-border);
}
</style>
