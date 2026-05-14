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
  <div>
    <h3 class="admin-title">
      Admin
    </h3>

    <div v-if="error" class="message message--error">
      {{ error }}
    </div>

    <div v-if="success" class="message message--success">
      {{ success }}
    </div>

    <div class="admin-group">
      <h4 class="group-title">Run Lottery</h4>
      <p class="group-desc">
        Select winners based on the random number. This cannot be undone.
      </p>
      <BaseButton
        :disabled="loading"
        state="danger"
        title="Run Lottery"
        size="small"
        @clicked="runLottery"
      />
    </div>

    <div class="admin-group">
      <h4 class="group-title">Award Guesses</h4>
      <p class="group-desc">
        Give additional guesses to specific members.
      </p>

      <div class="field">
        <label for="award-reason">Reason</label>
        <input
          id="award-reason"
          v-model="awardReason"
          placeholder="e.g., Event participation reward"
          type="text"
        >
      </div>

      <div class="field">
        <label for="award-users">Members</label>
        <div class="autocomplete-wrap">
          <input
            id="award-users"
            v-model="awardUserInput"
            placeholder="Start typing a name..."
            type="text"
            @blur="handleInputBlur"
            @focus="handleInputFocus"
          >
          <div v-if="showSuggestions && filteredMembers.length > 0" class="suggestions">
            <div
              v-for="member in filteredMembers"
              :key="member"
              class="suggestion"
              @click="addUser(member)"
            >
              {{ member }}
            </div>
          </div>
        </div>

        <div v-if="selectedUsers.length > 0" class="chips">
          <div v-for="user in selectedUsers" :key="user" class="user-chip">
            <span>{{ user }}</span>
            <button class="chip-remove" type="button" @click="removeUser(user)">
              &times;
            </button>
          </div>
        </div>
      </div>

      <BaseButton
        :disabled="loading"
        state="tertiary"
        title="Award Guesses"
        size="small"
        @clicked="awardUsers"
      />
    </div>
  </div>
</template>

<style scoped>
.admin-title {
  margin: 0 0 1rem 0;
  font-size: 1rem;
  font-weight: 600;
  color: var(--fg);
}

.message {
  padding: 0.75rem 1rem;
  border-radius: 12px;
  margin-bottom: 0.75rem;
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

.admin-group {
  padding: 1rem 0;
  border-bottom: 1px solid var(--border);
}

.admin-group:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.group-title {
  margin: 0 0 0.25rem 0;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--fg);
}

.group-desc {
  margin: 0 0 0.75rem 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.5;
}

.field {
  margin-bottom: 0.75rem;
}

.field label {
  display: block;
  margin-bottom: 0.375rem;
  font-weight: 500;
  color: var(--fg);
  font-size: 0.8rem;
}

.field input {
  width: 100%;
  font-size: 0.875rem;
}

.autocomplete-wrap {
  position: relative;
}

.suggestions {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 8px;
  margin-top: 0.25rem;
  max-height: 160px;
  overflow-y: auto;
  box-shadow: var(--elev);
  z-index: 10;
}

.suggestion {
  padding: 0.5rem 0.75rem;
  cursor: pointer;
  color: var(--fg);
  font-size: 0.875rem;
  transition: background 200ms ease;
}

.suggestion:hover {
  background: color-mix(in oklab, var(--link) 8%, transparent);
}

.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  margin-top: 0.5rem;
}

.user-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.25rem 0.625rem;
  background: var(--link);
  color: var(--bg);
  border-radius: 999px;
  font-size: 0.8rem;
}

.chip-remove {
  background: none;
  border: none;
  color: var(--bg);
  font-size: 1rem;
  line-height: 1;
  cursor: pointer;
  padding: 0;
  width: 1rem;
  height: 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: background 200ms ease;
}

.chip-remove:hover {
  background: rgba(255, 255, 255, 0.2);
}
</style>
