<script setup lang="ts">
import type { EventParticipant, FCEvent, Role } from '@/features/events/events.types'
import { computed, onMounted, ref } from 'vue'
import BaseModal from '@/components/BaseModal.vue'
import { useMembers } from '@/composables/useMembers'
import { EventsApi } from '@/features/events/events.api'
import { ROLE } from '@/features/events/events.types'

const modelValue = defineModel<boolean>('isOpen', { required: true })
const eventValue = defineModel<FCEvent>('fcEvent', { required: true })

const { members, load: memberLoad } = useMembers()
const participants = ref<EventParticipant[]>([])
const saving = ref(false)
const selectionMode = ref<'simple' | 'role'>('role') // Toggle between simple and role-based selection

// Load members when component mounts
onMounted(() => {
  if (members.value.length === 0) {
    memberLoad()
  }
  // Initialize participants from event
  participants.value = [...(eventValue.value.Participants || [])]
})

// Get the role assigned to a member, or null if not assigned
function getMemberRole(discordId: string): Role | null {
  const participant = participants.value.find(p => p.DiscordUserId === discordId)
  return participant?.Role ?? null
}

// Check if a member is selected (has any role assigned)
function isMemberSelected(discordId: string): boolean {
  return participants.value.some(p => p.DiscordUserId === discordId)
}

// Toggle selection mode
function toggleSelectionMode() {
  const newMode = selectionMode.value === 'simple' ? 'role' : 'simple'

  // Clear participants when switching modes to avoid confusion
  if (confirm(`Switching to ${newMode} mode will clear current selections. Continue?`)) {
    participants.value = []
    selectionMode.value = newMode
  }
}

// Toggle simple selection (for simple mode)
function toggleSimpleSelection(discordId: string) {
  const existingIndex = participants.value.findIndex(p => p.DiscordUserId === discordId)

  if (existingIndex >= 0) {
    // Remove the participant
    participants.value.splice(existingIndex, 1)
  }
  else {
    // Check if we've reached the limit
    if (participants.value.length >= eventValue.value.MaxNumberOfParticipants) {
      alert(`You can only select up to ${eventValue.value.MaxNumberOfParticipants} members.`)
      return
    }
    // Add new participant without a specific role (use Tank as default/placeholder)
    participants.value.push({
      DiscordUserId: discordId,
      Role: ROLE.Tank, // Placeholder role for simple mode
    })
  }
}

// Toggle role for a member (for role-based mode)
function toggleRole(discordId: string, role: Role) {
  const existingIndex = participants.value.findIndex(p => p.DiscordUserId === discordId)

  if (existingIndex >= 0) {
    const currentRole = participants.value[existingIndex].Role
    if (currentRole === role) {
      // If clicking the same role, remove the participant
      participants.value.splice(existingIndex, 1)
    }
    else {
      // If clicking a different role, update it
      participants.value[existingIndex].Role = role
    }
  }
  else {
    // Check if we've reached the limit
    if (participants.value.length >= eventValue.value.MaxNumberOfParticipants) {
      alert(`You can only select up to ${eventValue.value.MaxNumberOfParticipants} members.`)
      return
    }
    // Add new participant with this role
    participants.value.push({
      DiscordUserId: discordId,
      Role: role,
    })
  }
}

// Get the roles a member signed up for
function getMemberSignupRoles(discordId: string): Role[] {
  const signup = eventValue.value.Signups?.find(s => s.DiscordUserId === discordId)
  return signup?.Roles ?? []
}

// Check if a member signed up for a specific role
function hasSignedUpForRole(discordId: string, role: Role): boolean {
  const signupRoles = getMemberSignupRoles(discordId)
  return signupRoles.includes(role)
}

// Filter members to only show those who signed up
const signedUpMembers = computed(() => {
  if (!eventValue.value.Signups || eventValue.value.Signups.length === 0) {
    return []
  }

  return members.value.filter(member =>
    eventValue.value.Signups.some(signup => signup.DiscordUserId === member.DiscordId),
  )
})

// Count participants by role
const roleCount = computed(() => {
  return {
    [ROLE.Tank]: participants.value.filter(p => p.Role === ROLE.Tank).length,
    [ROLE.Healer]: participants.value.filter(p => p.Role === ROLE.Healer).length,
    [ROLE.Melee]: participants.value.filter(p => p.Role === ROLE.Melee).length,
    [ROLE.Caster]: participants.value.filter(p => p.Role === ROLE.Caster).length,
    [ROLE.Ranged]: participants.value.filter(p => p.Role === ROLE.Ranged).length,
  }
})

// Save participants to event
async function save() {
  saving.value = true
  try {
    const updatedEvent: FCEvent = {
      ...eventValue.value,
      Participants: participants.value,
    }
    await EventsApi.update(eventValue.value.Id, updatedEvent)
  }
  catch (error) {
    console.error('Error saving participants:', error)
    alert('Error saving participants. Please try again.')
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <BaseModal v-model="modelValue" title="Organize Event" :close-on-outside-click="false">
    <template #image>
      <img v-if="eventValue.PictureUrl" :src="eventValue.PictureUrl" alt="avatar" class="card__image">
    </template>
    <template #body>
      <!-- Mode Toggle Button -->
      <div class="mode-toggle-container">
        <button class="btn secondary mode-toggle-btn" @click="toggleSelectionMode">
          <span v-if="selectionMode === 'role'">Switch to Simple Mode</span>
          <span v-else>Switch to Role-Based Mode</span>
        </button>
      </div>

      <p v-if="selectionMode === 'role'">
        Select up to <b>{{ eventValue.MaxNumberOfParticipants }}</b> members who should participate in the Event '<b>{{ eventValue.Name }}</b>' and assign roles. Click '<b>Save</b>' once you are done.
      </p>
      <p v-else>
        Select up to <b>{{ eventValue.MaxNumberOfParticipants }}</b> members who should participate in the Event '<b>{{ eventValue.Name }}</b>'. Click '<b>Save</b>' once you are done.
      </p>
      <p class="muted" style="font-size: 0.9rem; margin-bottom: 1rem;">
        The bot will automatically post a new message in <b>#upcoming-roster</b>.
      </p>

      <!-- Summary of selected participants -->
      <div v-if="participants.length > 0" class="participants-summary">
        <h4>Selected Participants ({{ participants.length }} / {{ eventValue.MaxNumberOfParticipants }})</h4>
        <div v-if="selectionMode === 'role'" class="role-counts">
          <span class="role-badge">Tank: {{ roleCount[ROLE.Tank] }}</span>
          <span class="role-badge">Healer: {{ roleCount[ROLE.Healer] }}</span>
          <span class="role-badge">Melee: {{ roleCount[ROLE.Melee] }}</span>
          <span class="role-badge">Caster: {{ roleCount[ROLE.Caster] }}</span>
          <span class="role-badge">Ranged: {{ roleCount[ROLE.Ranged] }}</span>
        </div>
      </div>

      <!-- Member selection list -->
      <div class="member-list">
        <div v-for="member in signedUpMembers" :key="member.DiscordId" class="member-item">
          <div class="member-info">
            <img v-if="member.DiscordAvatar" :src="member.DiscordAvatar" alt="avatar" class="avatar">
            <div v-else class="avatar placeholder">
              {{ (member.PlayerName || member.DiscordName).charAt(0).toUpperCase() }}
            </div>
            <span class="member-name">{{ member.PlayerName || member.DiscordName }}</span>
          </div>

          <!-- Simple Mode: Single Select Button -->
          <div v-if="selectionMode === 'simple'" class="simple-selection">
            <button
              class="btn-select"
              :class="{ selected: isMemberSelected(member.DiscordId) }"
              :disabled="!isMemberSelected(member.DiscordId) && participants.length >= eventValue.MaxNumberOfParticipants"
              @click="toggleSimpleSelection(member.DiscordId)"
            >
              <span v-if="isMemberSelected(member.DiscordId)">✓ Selected</span>
              <span v-else>Select</span>
            </button>
          </div>

          <!-- Role Mode: Role Buttons -->
          <div v-else class="role-buttons">
            <button
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === ROLE.Tank }"
              :disabled="!hasSignedUpForRole(member.DiscordId, ROLE.Tank) || (!isMemberSelected(member.DiscordId) && participants.length >= eventValue.MaxNumberOfParticipants)"
              title="Tank"
              @click="toggleRole(member.DiscordId, ROLE.Tank)"
            >
              T
            </button>
            <button
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === ROLE.Healer }"
              :disabled="!hasSignedUpForRole(member.DiscordId, ROLE.Healer) || (!isMemberSelected(member.DiscordId) && participants.length >= eventValue.MaxNumberOfParticipants)"
              title="Healer"
              @click="toggleRole(member.DiscordId, ROLE.Healer)"
            >
              H
            </button>
            <button
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === ROLE.Melee }"
              :disabled="!hasSignedUpForRole(member.DiscordId, ROLE.Melee) || (!isMemberSelected(member.DiscordId) && participants.length >= eventValue.MaxNumberOfParticipants)"
              title="Melee"
              @click="toggleRole(member.DiscordId, ROLE.Melee)"
            >
              M
            </button>
            <button
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === ROLE.Caster }"
              :disabled="!hasSignedUpForRole(member.DiscordId, ROLE.Caster) || (!isMemberSelected(member.DiscordId) && participants.length >= eventValue.MaxNumberOfParticipants)"
              title="Caster"
              @click="toggleRole(member.DiscordId, ROLE.Caster)"
            >
              C
            </button>
            <button
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === ROLE.Ranged }"
              :disabled="!hasSignedUpForRole(member.DiscordId, ROLE.Ranged) || (!isMemberSelected(member.DiscordId) && participants.length >= eventValue.MaxNumberOfParticipants)"
              title="Ranged"
              @click="toggleRole(member.DiscordId, ROLE.Ranged)"
            >
              R
            </button>
          </div>
        </div>
      </div>
    </template>
    <template #actions>
      <button class="btn secondary" :disabled="saving" @click="emit('update:modelValue', false)">
        Cancel
      </button>
      <button class="btn primary" :disabled="saving" @click="save">
        {{ saving ? 'Saving...' : 'Save' }}
      </button>
    </template>
  </BaseModal>
</template>

<style scoped>
.participants-summary {
  background: var(--muted-bg);
  padding: 1rem;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.participants-summary h4 {
  margin: 0 0 0.5rem 0;
  font-size: 0.95rem;
  color: var(--fg);
}

.role-counts {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.role-badge {
  background: var(--card);
  border: 1px solid var(--border);
  padding: 0.25rem 0.5rem;
  border-radius: 6px;
  font-size: 0.85rem;
  color: var(--fg);
}

.member-list {
  max-height: 400px;
  overflow-y: auto;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
}

.member-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem;
  border-bottom: 1px solid var(--border);
  transition: background 0.2s;
}

.member-item:last-child {
  border-bottom: none;
}

.member-item:hover {
  background: var(--muted-bg);
}

.member-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
}

.member-name {
  font-weight: 500;
  color: var(--fg);
}

.role-buttons {
  display: flex;
  gap: 0.25rem;
}

.btn-role {
  width: 32px;
  height: 32px;
  border-radius: 6px;
  border: 1px solid var(--border);
  background: var(--card);
  color: var(--muted);
  cursor: pointer;
  font-weight: 600;
  font-size: 0.85rem;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-role:hover {
  background: var(--muted-bg);
  border-color: var(--link);
  color: var(--link);
}

.btn-role.active {
  background: var(--btn-success-bg);
  color: var(--btn-success-fg);
  border-color: var(--btn-success-bg);
}

.btn-role:focus {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.btn-role:disabled {
  opacity: 0.4;
  cursor: not-allowed;
  background: var(--muted-bg);
  color: var(--muted);
  border-color: var(--border);
}

.btn-role:disabled:hover {
  background: var(--muted-bg);
  border-color: var(--border);
  color: var(--muted);
  transform: none;
}

/* Scrollbar styling */
.member-list::-webkit-scrollbar {
  width: 8px;
}

.member-list::-webkit-scrollbar-track {
  background: var(--muted-bg);
  border-radius: 4px;
}

.member-list::-webkit-scrollbar-thumb {
  background: var(--border);
  border-radius: 4px;
}

.member-list::-webkit-scrollbar-thumb:hover {
  background: var(--muted);
}

/* Mode Toggle Styles */
.mode-toggle-container {
  display: flex;
  justify-content: center;
  margin-bottom: 1rem;
}

.mode-toggle-btn {
  font-size: 0.9rem;
  padding: 0.5rem 1rem;
}

/* Simple Selection Styles */
.simple-selection {
  display: flex;
  align-items: center;
}

.btn-select {
  padding: 0.5rem 1rem;
  border-radius: 6px;
  border: 1px solid var(--border);
  background: var(--card);
  color: var(--fg);
  cursor: pointer;
  font-weight: 600;
  font-size: 0.85rem;
  transition: all 0.2s;
  min-width: 100px;
}

.btn-select:hover:not(:disabled) {
  background: var(--muted-bg);
  border-color: var(--link);
  color: var(--link);
}

.btn-select.selected {
  background: var(--btn-success-bg);
  color: var(--btn-success-fg);
  border-color: var(--btn-success-bg);
}

.btn-select:disabled {
  opacity: 0.4;
  cursor: not-allowed;
  background: var(--muted-bg);
  color: var(--muted);
  border-color: var(--border);
}

.btn-select:focus {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}
</style>
