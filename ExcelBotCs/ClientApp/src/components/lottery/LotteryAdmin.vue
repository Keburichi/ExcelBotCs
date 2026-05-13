<script lang="ts" setup>
import { computed, onMounted, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import { LotteryApi } from '@/features/lottery/lottery.api'

const emit = defineEmits<{
  refresh: []
}>()

const loading = ref(false)
const error = ref('')
const success = ref('')
const awardReason = ref('')
const awardUserInput = ref('')
const selectedUsers = ref<string[]>([])
const fcMembers = ref<string[]>([])
const showSuggestions = ref(false)

const filteredMembers = computed(() => {
  if (!awardUserInput.value.trim())
    return []
  const search = awardUserInput.value.toLowerCase()
  return fcMembers.value.filter(name =>
    name.toLowerCase().includes(search)
    && !selectedUsers.value.includes(name),
  ).slice(0, 10)
})

onMounted(async () => {
  try {
    fcMembers.value = await LotteryApi.fcMembers()
  }
  catch (e: any) {
    console.error('Failed to load FC members:', e)
  }
})

function addUser(userName: string) {
  if (!selectedUsers.value.includes(userName)) {
    selectedUsers.value.push(userName)
    awardUserInput.value = ''
    showSuggestions.value = false
  }
}

function removeUser(userName: string) {
  selectedUsers.value = selectedUsers.value.filter(u => u !== userName)
}

function handleInputFocus() {
  showSuggestions.value = true
}

function handleInputBlur() {
  // Delay to allow click on suggestion
  setTimeout(() => showSuggestions.value = false, 200)
}

async function runLottery() {
  if (!confirm('Are you sure you want to run the lottery? This will select winners and cannot be undone.')) {
    return
  }

  loading.value = true
  error.value = ''
  success.value = ''

  try {
    const result = await LotteryApi.runLottery()
    success.value = result.message
    emit('refresh')
  }
  catch (e: any) {
    error.value = e.message || 'Failed to run lottery'
  }
  finally {
    loading.value = false
  }
}

async function awardUsers() {
  if (!awardReason.value.trim()) {
    error.value = 'Please enter a reason for awarding guesses'
    return
  }

  if (selectedUsers.value.length === 0) {
    error.value = 'Please select at least one user'
    return
  }

  loading.value = true
  error.value = ''
  success.value = ''

  try {
    const result = await LotteryApi.awardUsers(awardReason.value, selectedUsers.value)
    success.value = result.message
    awardReason.value = ''
    selectedUsers.value = []
    emit('refresh')
  }
  catch (e: any) {
    error.value = e.message || 'Failed to award users'
  }
  finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="admin-container">
    <h3 class="admin-title">
      Admin Actions
    </h3>

    <div v-if="error" class="message message--error">
      {{ error }}
    </div>

    <div v-if="success" class="message message--success">
      {{ success }}
    </div>

    <div class="admin-section">
      <h4 class="section-title">
        Run Lottery
      </h4>
      <p class="section-description">
        Execute the lottery to select winners based on the random number. This action cannot be undone.
      </p>
      <BaseButton
        :disabled="loading"
        state="danger"
        title="Run Lottery"
        @clicked="runLottery"
      />
    </div>

    <div class="admin-section">
      <h4 class="section-title">
        Award Guesses to Users
      </h4>
      <p class="section-description">
        Give additional guesses to specific users. Search and select FC members by name.
      </p>

      <div class="form-group">
        <label for="award-reason">Reason</label>
        <input
          id="award-reason"
          v-model="awardReason"
          placeholder="e.g., Event participation reward"
          type="text"
        >
      </div>

      <div class="form-group">
        <label for="award-users">Select Users</label>
        <div class="autocomplete-container">
          <input
            id="award-users"
            v-model="awardUserInput"
            placeholder="Start typing a member name..."
            type="text"
            @blur="handleInputBlur"
            @focus="handleInputFocus"
          >
          <div v-if="showSuggestions && filteredMembers.length > 0" class="autocomplete-suggestions">
            <div
              v-for="member in filteredMembers"
              :key="member"
              class="suggestion-item"
              @click="addUser(member)"
            >
              {{ member }}
            </div>
          </div>
        </div>

        <div v-if="selectedUsers.length > 0" class="selected-users">
          <div v-for="user in selectedUsers" :key="user" class="user-chip">
            <span>{{ user }}</span>
            <button class="remove-chip" type="button" @click="removeUser(user)">
              &times;
            </button>
          </div>
        </div>
      </div>

      <BaseButton
        :disabled="loading"
        state="tertiary"
        title="Award Guesses"
        @clicked="awardUsers"
      />
    </div>
  </div>
</template>

<style scoped>
.admin-container {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 16px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  padding: 1.5rem;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

:root[data-theme='dark'] .admin-container {
  background: rgba(18, 26, 45, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .admin-container {
    background: rgba(18, 26, 45, 0.7);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.admin-container:hover {
  backdrop-filter: blur(24px);
  border-color: rgba(59, 130, 246, 0.3);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.15),
  0 4px 16px rgba(0, 0, 0, 0.1),
  inset 0 1px 0 rgba(255, 255, 255, 0.6);
}

:root[data-theme='dark'] .admin-container:hover {
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
  0 4px 16px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .admin-container:hover {
    border-color: rgba(59, 130, 246, 0.4);
    box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
}

.admin-title {
  margin: 0 0 1.5rem 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--fg);
}

.admin-section {
  padding: 1.5rem;
  background: rgba(0, 0, 0, 0.02);
  border-radius: 16px;
  margin-bottom: 1.5rem;
  border: 1px solid var(--border);
}

[data-theme="dark"] .admin-section {
  background: rgba(255, 255, 255, 0.05);
}

.admin-section:last-child {
  margin-bottom: 0;
}

.section-title {
  margin: 0 0 0.5rem 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--fg);
}

.section-description {
  margin: 0 0 1rem 0;
  color: var(--muted);
  font-size: 0.875rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: var(--fg);
  font-size: 0.875rem;
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  font-family: inherit;
  font-size: 0.875rem;
  background: var(--card);
  color: var(--fg);
}

.form-group input:focus,
.form-group textarea:focus {
  outline: none;
  border-color: var(--link);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.form-group textarea {
  resize: vertical;
  min-height: 80px;
}

.autocomplete-container {
  position: relative;
}

.autocomplete-suggestions {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 8px;
  margin-top: 0.25rem;
  max-height: 200px;
  overflow-y: auto;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
  z-index: 10;
}

.suggestion-item {
  padding: 0.75rem;
  cursor: pointer;
  color: var(--fg);
  transition: background 0.2s;
}

.suggestion-item:hover {
  background: rgba(59, 130, 246, 0.1);
}

.selected-users {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.75rem;
}

.user-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  background: var(--link);
  color: var(--bg);
  border-radius: 9999px;
  font-size: 0.875rem;
}

.remove-chip {
  background: none;
  border: none;
  color: var(--bg);
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
  padding: 0;
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background 0.2s;
}

.remove-chip:hover {
  background: rgba(255, 255, 255, 0.2);
}

.message {
  padding: 1rem;
  border-radius: 0.5rem;
  margin-bottom: 1rem;
  font-size: 0.875rem;
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
