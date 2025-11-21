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
}>()

const eventsComposable = useEvents()
const membersComposable = useMembers()

// State
const awardLottery = ref(true)
const selectedParticipants = ref<string[]>([])
const searchQuery = ref('')
const isSubmitting = ref(false)

// Members list
const allMembers = computed(() => membersComposable.members.value)

// Get participant names from occurrence
const occurrenceParticipantNames = computed((): string[] => {
  if (!props.occurrence.Participants)
    return []

  return props.occurrence.Participants.map((participant) => {
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
    selectedParticipants.value = [...occurrenceParticipantNames.value]
    awardLottery.value = true
    searchQuery.value = ''
    isSubmitting.value = false
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

// Submit and conclude event
async function concludeEvent() {
  isSubmitting.value = true

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
  catch (error) {
    console.error('Error concluding event:', error)
    alert('Failed to conclude event. Please try again.')
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
    :title="`Conclude Event - ${event.Name}`"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <template #body>
      <div class="conclude-body">
        <div class="occurrence-info">
          <strong>Occurrence:</strong> {{ occurrenceDate }}
        </div>

        <div class="section">
          <label class="checkbox-label">
            <input v-model="awardLottery" type="checkbox">
            <span>Award lottery guesses to participants</span>
          </label>
        </div>

        <div v-if="awardLottery" class="section">
          <h3 class="section-title">
            Participants
          </h3>
          <p class="section-description">
            Adjust the list if needed. Only listed participants will receive lottery guesses.
          </p>

          <div class="participants-list">
            <div
              v-for="name in selectedParticipants"
              :key="name"
              class="participant-item"
            >
              <span class="participant-name">{{ name }}</span>
              <BaseButton
                size="small"
                state="danger"
                title="Remove"
                @clicked="removeMember(name)"
              />
            </div>
            <div v-if="selectedParticipants.length === 0" class="empty-message">
              No participants selected
            </div>
          </div>

          <div class="search-section">
            <label class="search-label">Add participant:</label>
            <input
              v-model="searchQuery"
              class="search-input"
              placeholder="Search members..."
              type="text"
            >
            <div v-if="filteredMembers.length > 0" class="search-results">
              <div
                v-for="member in filteredMembers"
                :key="member.Id"
                class="search-result-item"
                @click="addMember(member)"
              >
                {{ member.PlayerName || member.DiscordName }}
              </div>
            </div>
          </div>
        </div>

        <div class="warning-section">
          <p>⚠️ This will mark the occurrence as completed. This action cannot be undone.</p>
        </div>
      </div>
    </template>

    <template #actions>
      <BaseButton
        state="secondary"
        title="Cancel"
        @clicked="emit('update:modelValue', false)"
      />
      <BaseButton
        :disabled="isSubmitting"
        :title="isSubmitting ? 'Processing...' : 'Conclude Event'"
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
  gap: 20px;
}

.occurrence-info {
  padding: 12px;
  background: var(--muted-bg, #f9f9f9);
  border-radius: 8px;
  border: 1px solid var(--border, #e0e0e0);
  font-size: 0.9rem;
}

.occurrence-info strong {
  color: var(--muted, #666);
  margin-right: 8px;
}

.section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
}

.checkbox-label input[type="checkbox"] {
  width: 18px;
  height: 18px;
  cursor: pointer;
}

.section-title {
  font-size: 1.1rem;
  font-weight: 600;
  margin: 0;
  color: var(--fg, #333);
}

.section-description {
  font-size: 0.9rem;
  color: var(--muted, #666);
  margin: 0;
}

.participants-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 250px;
  overflow-y: auto;
  padding: 12px;
  background: var(--muted-bg, #f9f9f9);
  border-radius: 8px;
  border: 1px solid var(--border, #e0e0e0);
}

.participant-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: white;
  border-radius: 6px;
  border: 1px solid var(--border, #e0e0e0);
}

.participant-name {
  font-size: 0.95rem;
  font-weight: 500;
}

.empty-message {
  text-align: center;
  color: var(--muted, #666);
  font-style: italic;
  padding: 20px;
}

.search-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
  position: relative;
}

.search-label {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--fg, #333);
}

.search-input {
  padding: 10px 12px;
  font-size: 0.95rem;
  border: 1px solid var(--border, #e0e0e0);
  border-radius: 6px;
  background: white;
  color: var(--fg, #333);
  outline: none;
  transition: border-color 0.2s;
}

.search-input:focus {
  border-color: var(--primary, #007bff);
}

.search-results {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: white;
  border: 1px solid var(--border, #e0e0e0);
  border-radius: 6px;
  margin-top: 4px;
  max-height: 200px;
  overflow-y: auto;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  z-index: 1000;
}

.search-result-item {
  padding: 10px 12px;
  cursor: pointer;
  font-size: 0.95rem;
  transition: background-color 0.2s;
}

.search-result-item:hover {
  background: var(--muted-bg, #f9f9f9);
}

.search-result-item:not(:last-child) {
  border-bottom: 1px solid var(--border, #e0e0e0);
}

.warning-section {
  padding: 12px;
  background: #fff3cd;
  border: 1px solid #ffc107;
  border-radius: 8px;
}

.warning-section p {
  margin: 0;
  color: #856404;
  font-size: 0.9rem;
  font-weight: 500;
}
</style>
