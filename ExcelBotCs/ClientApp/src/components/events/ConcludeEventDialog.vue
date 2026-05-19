<script lang="ts" setup>
import type { EventOccurrence, FCEvent } from '@/features/events/events.types'
import type { Member } from '@/features/members/members.types'
import { computed, onMounted, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import { useEvents } from '@/composables/useEvents'
import { useMembers } from '@/composables/useMembers'
import { OccurrenceStatus } from '@/features/events/events.types'
import { LotteryApi } from '@/features/lottery/lottery.api'

const props = defineProps<{
  modelValue: boolean
  event: FCEvent
  occurrence: EventOccurrence
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'concluded'): void
  (e: 'skipped'): void
}>()

const eventsComposable = useEvents()
const membersComposable = useMembers()

// State
const awardLottery = ref(true)
const selectedParticipants = ref<string[]>([])
const searchQuery = ref('')
const isSubmitting = ref(false)
const errorMessage = ref('')

// Members list
const allMembers = computed(() => membersComposable.members.value)

// Get participant names from event groups
const eventParticipantNames = computed((): string[] => {
  if (!props.event.Groups || props.event.Groups.length === 0)
    return []

  return props.event.Groups.flatMap(group => group.Participants).map((participant) => {
    const member = allMembers.value.find(m => m.DiscordId === participant.DiscordUserId)
    return member?.PlayerName || member?.DiscordName || participant.DiscordUserId
  })
})

// Filtered members for search
const filteredMembers = computed(() => {
  if (!searchQuery.value)
    return []

  const query = searchQuery.value.toLowerCase()
  return allMembers.value
    .filter((m) => {
      const name = (m.PlayerName || m.DiscordName || '').toLowerCase()
      return name.includes(query) && !selectedParticipants.value.includes(m.PlayerName || m.DiscordName || '')
    })
    .slice(0, 10) // Limit to 10 results
})

// Format occurrence date
const occurrenceDate = computed(() => {
  return new Date(props.occurrence.OccurrenceDate).toLocaleString(undefined, {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
})

// Initialize selected participants when dialog opens
watch(() => props.modelValue, (isOpen) => {
  if (isOpen) {
    selectedParticipants.value = [...eventParticipantNames.value]
    awardLottery.value = true
    searchQuery.value = ''
    isSubmitting.value = false
    errorMessage.value = ''
  }
})

// Add member to participants
function addMember(member: Member) {
  const name = member.PlayerName || member.DiscordName
  if (name && !selectedParticipants.value.includes(name)) {
    selectedParticipants.value.push(name)
  }
  searchQuery.value = ''
}

// Remove member from participants
function removeMember(name: string) {
  selectedParticipants.value = selectedParticipants.value.filter(n => n !== name)
}

// Submit and conclude event (mark as Completed)
async function concludeEvent() {
  isSubmitting.value = true
  errorMessage.value = ''

  try {
    // Update occurrence status to Completed
    await eventsComposable.updateOccurrenceStatusById(
      props.event.Id,
      props.occurrence.Id,
      OccurrenceStatus.Completed,
    )

    // Award lottery guesses if enabled and there are participants
    if (awardLottery.value && selectedParticipants.value.length > 0) {
      await LotteryApi.awardUsers(props.event.Name, selectedParticipants.value)
    }

    // Close dialog and notify parent
    emit('update:modelValue', false)
    emit('concluded')
  }
  catch (error: any) {
    console.error('Error concluding event:', error)
    errorMessage.value = error?.message || 'Failed to conclude event. Please try again.'
  }
  finally {
    isSubmitting.value = false
  }
}

// Skip occurrence (mark as Cancelled without awarding lottery)
async function skipOccurrence() {
  isSubmitting.value = true
  errorMessage.value = ''

  try {
    // Update occurrence status to Cancelled
    await eventsComposable.updateOccurrenceStatusById(
      props.event.Id,
      props.occurrence.Id,
      OccurrenceStatus.Cancelled,
    )

    // Close dialog and notify parent
    emit('update:modelValue', false)
    emit('skipped')
  }
  catch (error: any) {
    console.error('Error skipping occurrence:', error)
    errorMessage.value = error?.message || 'Failed to skip occurrence. Please try again.'
  }
  finally {
    isSubmitting.value = false
  }
}

// Load members on mount
onMounted(() => {
  if (membersComposable.members.value.length === 0) {
    membersComposable.load()
  }
})
</script>

<template>
  <BaseModal
    :model-value="props.modelValue"
    :title="`Conclude - ${event.Name}`"
    size="small"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <template #body>
      <div class="conclude-body">
        <div class="occurrence-badge">
          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
            <line x1="16" y1="2" x2="16" y2="6" />
            <line x1="8" y1="2" x2="8" y2="6" />
            <line x1="3" y1="10" x2="21" y2="10" />
          </svg>
          <span>{{ occurrenceDate }}</span>
        </div>

        <label class="lottery-toggle">
          <input v-model="awardLottery" type="checkbox">
          <span class="toggle-label">Award lottery guesses</span>
          <span class="toggle-count">{{ selectedParticipants.length }} participant{{ selectedParticipants.length !== 1 ? 's' : '' }}</span>
        </label>

        <div v-if="awardLottery" class="participants-section">
          <div class="participants-list">
            <div
              v-for="name in selectedParticipants"
              :key="name"
              class="participant-chip"
            >
              <span>{{ name }}</span>
              <button class="chip-remove" type="button" @click="removeMember(name)">&times;</button>
            </div>
            <div v-if="selectedParticipants.length === 0" class="empty-state">
              No participants selected
            </div>
          </div>

          <div class="search-wrapper">
            <input
              v-model="searchQuery"
              class="search-input"
              placeholder="Add participant..."
              type="text"
            >
            <div v-if="filteredMembers.length > 0" class="search-dropdown">
              <div
                v-for="member in filteredMembers"
                :key="member.Id"
                class="search-option"
                @click="addMember(member)"
              >
                {{ member.PlayerName || member.DiscordName }}
              </div>
            </div>
          </div>
        </div>

        <div v-if="errorMessage" class="error-banner">
          <p>{{ errorMessage }}</p>
        </div>

        <div class="action-hint">
          <p><strong>Conclude</strong> marks as completed and awards lottery guesses.</p>
          <p><strong>Skip</strong> marks as cancelled. No lottery awarded.</p>
        </div>
      </div>
    </template>

    <template #actions>
      <BaseButton
        state="secondary"
        title="Close"
        @clicked="emit('update:modelValue', false)"
      />
      <BaseButton
        :disabled="isSubmitting"
        :title="isSubmitting ? 'Skipping...' : 'Skip'"
        state="warning"
        @clicked="skipOccurrence"
      />
      <BaseButton
        :disabled="isSubmitting"
        :title="isSubmitting ? 'Concluding...' : 'Conclude'"
        state="primary"
        @clicked="concludeEvent"
      />
    </template>
  </BaseModal>
</template>

<style scoped>
.conclude-body {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.occurrence-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  background: var(--muted-bg);
  border: 1px solid var(--border);
  border-radius: 8px;
  font-size: 0.85rem;
  color: var(--muted);
  width: fit-content;
}

.lottery-toggle {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-direction: row;
  cursor: pointer;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  border: 1px solid var(--border);
  background: var(--muted-bg);
  transition: border-color 0.2s;
}

.lottery-toggle:hover {
  border-color: var(--link);
}

.lottery-toggle input[type="checkbox"] {
  width: 1rem;
  height: 1rem;
  cursor: pointer;
  flex-shrink: 0;
}

.toggle-label {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--fg);
}

.toggle-count {
  margin-left: auto;
  font-size: 0.8rem;
  color: var(--muted);
}

.participants-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.participants-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
  max-height: 180px;
  overflow-y: auto;
  padding: 0.75rem;
  background: var(--muted-bg);
  border-radius: 8px;
  border: 1px solid var(--border);
}

.participant-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.5rem;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  color: var(--fg);
  transition: border-color 0.2s;
}

.participant-chip:hover {
  border-color: var(--danger, #ef4444);
}

.chip-remove {
  background: none;
  border: none;
  font-size: 1rem;
  line-height: 1;
  cursor: pointer;
  color: var(--muted);
  padding: 0 0.125rem;
  transition: color 0.2s;
}

.chip-remove:hover {
  color: var(--danger, #ef4444);
}

.empty-state {
  width: 100%;
  text-align: center;
  color: var(--muted);
  font-size: 0.85rem;
  padding: 1.5rem 0;
}

.search-wrapper {
  position: relative;
}

.search-input {
  width: 100%;
  font-size: 0.85rem;
}

.search-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 8px;
  max-height: 180px;
  overflow-y: auto;
  box-shadow: var(--elev);
  z-index: 10;
}

.search-option {
  padding: 0.5rem 0.75rem;
  cursor: pointer;
  font-size: 0.85rem;
  transition: background 0.15s;
}

.search-option:hover {
  background: var(--muted-bg);
}

.search-option:not(:last-child) {
  border-bottom: 1px solid var(--border);
}

.error-banner {
  padding: 0.75rem 1rem;
  background: var(--alert-error-bg);
  border: 1px solid var(--alert-error-border);
  border-radius: 8px;
}

.error-banner p {
  margin: 0;
  color: var(--alert-error-fg);
  font-size: 0.85rem;
  font-weight: 500;
}

.action-hint {
  padding: 0.75rem 1rem;
  background: var(--muted-bg);
  border: 1px solid var(--border);
  border-radius: 8px;
}

.action-hint p {
  margin: 0;
  color: var(--muted);
  font-size: 0.8rem;
  line-height: 1.5;
}

.action-hint p + p {
  margin-top: 0.25rem;
}
</style>
