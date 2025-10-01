<script setup lang="ts">

import {FCEvent, EventParticipant, Role} from "@/features/events/events.types";
import BaseModal from "@/components/BaseModal.vue";
import {useMembers} from "@/features/members/useMembers";
import {computed, onMounted, ref} from "vue";
import {EventsApi} from "@/features/events/events.api";

const props = defineProps<{
  modelValue: boolean
  event: FCEvent
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>();

const members = useMembers();
const participants = ref<EventParticipant[]>([]);
const saving = ref(false);
const selectionMode = ref<'simple' | 'role'>('role'); // Toggle between simple and role-based selection
const MAX_SIMPLE_SELECTION = 8;

// Load members when component mounts
onMounted(() => {
  if (members.members.value.length === 0) {
    members.load();
  }
  // Initialize participants from event
  participants.value = [...(props.event.Participants || [])];
});

// Get the role assigned to a member, or null if not assigned
function getMemberRole(discordId: string): Role | null {
  const participant = participants.value.find(p => p.DiscordUserId === discordId);
  return participant?.Role ?? null;
}

// Check if a member is selected (has any role assigned)
function isMemberSelected(discordId: string): boolean {
  return participants.value.some(p => p.DiscordUserId === discordId);
}

// Toggle selection mode
function toggleSelectionMode() {
  const newMode = selectionMode.value === 'simple' ? 'role' : 'simple';
  
  // Clear participants when switching modes to avoid confusion
  if (confirm(`Switching to ${newMode} mode will clear current selections. Continue?`)) {
    participants.value = [];
    selectionMode.value = newMode;
  }
}

// Toggle simple selection (for simple mode)
function toggleSimpleSelection(discordId: string) {
  const existingIndex = participants.value.findIndex(p => p.DiscordUserId === discordId);
  
  if (existingIndex >= 0) {
    // Remove the participant
    participants.value.splice(existingIndex, 1);
  } else {
    // Check if we've reached the limit
    if (participants.value.length >= MAX_SIMPLE_SELECTION) {
      alert(`You can only select up to ${MAX_SIMPLE_SELECTION} members in simple mode.`);
      return;
    }
    // Add new participant without a specific role (use Tank as default/placeholder)
    participants.value.push({
      DiscordUserId: discordId,
      Role: Role.Tank // Placeholder role for simple mode
    });
  }
}

// Toggle role for a member (for role-based mode)
function toggleRole(discordId: string, role: Role) {
  const existingIndex = participants.value.findIndex(p => p.DiscordUserId === discordId);
  
  if (existingIndex >= 0) {
    const currentRole = participants.value[existingIndex].Role;
    if (currentRole === role) {
      // If clicking the same role, remove the participant
      participants.value.splice(existingIndex, 1);
    } else {
      // If clicking a different role, update it
      participants.value[existingIndex].Role = role;
    }
  } else {
    // Add new participant with this role
    participants.value.push({
      DiscordUserId: discordId,
      Role: role
    });
  }
}

// Remove a member from participants (deselect)
function removeMember(discordId: string) {
  participants.value = participants.value.filter(p => p.DiscordUserId !== discordId);
}

// Get display name for a member
function getDisplayName(discordId: string): string {
  const member = members.members.value.find(m => m.DiscordId === discordId);
  if (!member) return discordId;
  return member.PlayerName || member.DiscordName;
}

// Get the roles a member signed up for
function getMemberSignupRoles(discordId: string): Role[] {
  const signup = props.event.Signups?.find(s => s.DiscordUserId === discordId);
  return signup?.Roles ?? [];
}

// Check if a member signed up for a specific role
function hasSignedUpForRole(discordId: string, role: Role): boolean {
  const signupRoles = getMemberSignupRoles(discordId);
  return signupRoles.includes(role);
}

// Filter members to only show those who signed up
const signedUpMembers = computed(() => {
  if (!props.event.Signups || props.event.Signups.length === 0) {
    return [];
  }
  
  return members.members.value.filter(member => 
    props.event.Signups.some(signup => signup.DiscordUserId === member.DiscordId)
  );
});

// Count participants by role
const roleCount = computed(() => {
  return {
    [Role.Tank]: participants.value.filter(p => p.Role === Role.Tank).length,
    [Role.Healer]: participants.value.filter(p => p.Role === Role.Healer).length,
    [Role.Melee]: participants.value.filter(p => p.Role === Role.Melee).length,
    [Role.Caster]: participants.value.filter(p => p.Role === Role.Caster).length,
    [Role.Ranged]: participants.value.filter(p => p.Role === Role.Ranged).length,
  };
});

// Save participants to event
async function save() {
  saving.value = true;
  try {
    const updatedEvent: FCEvent = {
      ...props.event,
      Participants: participants.value
    };
    await EventsApi.update(props.event.Id, updatedEvent);
    emit('update:modelValue', false);
  } catch (error) {
    console.error('Error saving participants:', error);
    alert('Error saving participants. Please try again.');
  } finally {
    saving.value = false;
  }
}

// Role enum for template
const roleEnum = Role;

</script>

<template>
  <BaseModal :modelValue="props.modelValue" @update:modelValue="emit('update:modelValue', $event)" :title="'Organize Event'" :closeOnOutsideClick="false">
    <template #image>
      <img v-if="event.PictureUrl" :src="event.PictureUrl" alt="avatar" class="card__image">
    </template>
    <template #body>
      <!-- Mode Toggle Button -->
      <div class="mode-toggle-container">
        <button class="btn secondary mode-toggle-btn" @click="toggleSelectionMode">
          <span v-if="selectionMode === 'role'">Switch to Simple Mode</span>
          <span v-else>Switch to Role-Based Mode</span>
        </button>
      </div>
      
      <p v-if="selectionMode === 'role'">Select the members who should participate in the Event '<b>{{ event.Name }}</b>' and assign roles. Click '<b>Save</b>' once you are done.</p>
      <p v-else>Select up to <b>{{ MAX_SIMPLE_SELECTION }}</b> members who should participate in the Event '<b>{{ event.Name }}</b>'. Click '<b>Save</b>' once you are done.</p>
      <p class="muted" style="font-size: 0.9rem; margin-bottom: 1rem;">The bot will automatically post a new message in <b>#upcoming-roster</b>.</p>
      
      <!-- Summary of selected participants -->
      <div v-if="participants.length > 0" class="participants-summary">
        <h4>Selected Participants ({{ participants.length }}<span v-if="selectionMode === 'simple'"> / {{ MAX_SIMPLE_SELECTION }}</span>)</h4>
        <div v-if="selectionMode === 'role'" class="role-counts">
          <span class="role-badge">Tank: {{ roleCount[roleEnum.Tank] }}</span>
          <span class="role-badge">Healer: {{ roleCount[roleEnum.Healer] }}</span>
          <span class="role-badge">Melee: {{ roleCount[roleEnum.Melee] }}</span>
          <span class="role-badge">Caster: {{ roleCount[roleEnum.Caster] }}</span>
          <span class="role-badge">Ranged: {{ roleCount[roleEnum.Ranged] }}</span>
        </div>
      </div>

      <!-- Member selection list -->
      <div class="member-list">
        <div v-for="member in signedUpMembers" :key="member.DiscordId" class="member-item">
          <div class="member-info">
            <img v-if="member.DiscordAvatar" :src="member.DiscordAvatar" alt="avatar" class="avatar">
            <div v-else class="avatar placeholder">{{ (member.PlayerName || member.DiscordName).charAt(0).toUpperCase() }}</div>
            <span class="member-name">{{ member.PlayerName || member.DiscordName }}</span>
          </div>
          
          <!-- Simple Mode: Single Select Button -->
          <div v-if="selectionMode === 'simple'" class="simple-selection">
            <button 
              class="btn-select"
              :class="{ selected: isMemberSelected(member.DiscordId) }"
              :disabled="!isMemberSelected(member.DiscordId) && participants.length >= MAX_SIMPLE_SELECTION"
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
              :class="{ active: getMemberRole(member.DiscordId) === roleEnum.Tank }"
              :disabled="!hasSignedUpForRole(member.DiscordId, roleEnum.Tank)"
              @click="toggleRole(member.DiscordId, roleEnum.Tank)"
              title="Tank"
            >
              T
            </button>
            <button 
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === roleEnum.Healer }"
              :disabled="!hasSignedUpForRole(member.DiscordId, roleEnum.Healer)"
              @click="toggleRole(member.DiscordId, roleEnum.Healer)"
              title="Healer"
            >
              H
            </button>
            <button 
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === roleEnum.Melee }"
              :disabled="!hasSignedUpForRole(member.DiscordId, roleEnum.Melee)"
              @click="toggleRole(member.DiscordId, roleEnum.Melee)"
              title="Melee"
            >
              M
            </button>
            <button 
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === roleEnum.Caster }"
              :disabled="!hasSignedUpForRole(member.DiscordId, roleEnum.Caster)"
              @click="toggleRole(member.DiscordId, roleEnum.Caster)"
              title="Caster"
            >
              C
            </button>
            <button 
              class="btn-role"
              :class="{ active: getMemberRole(member.DiscordId) === roleEnum.Ranged }"
              :disabled="!hasSignedUpForRole(member.DiscordId, roleEnum.Ranged)"
              @click="toggleRole(member.DiscordId, roleEnum.Ranged)"
              title="Ranged"
            >
              R
            </button>
          </div>
        </div>
      </div>
    </template>
  <template #actions>
    <button class="btn secondary" @click="emit('update:modelValue', false)" :disabled="saving">Cancel</button>
    <button class="btn primary" @click="save" :disabled="saving">{{ saving ? 'Saving...' : 'Save' }}</button>
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