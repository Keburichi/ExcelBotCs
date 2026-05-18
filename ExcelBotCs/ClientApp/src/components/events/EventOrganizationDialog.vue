<script setup lang="ts">
import type { EventGroupRequest, EventParticipant, EventSignup, FCEvent, Role } from '@/features/events/events.types'
import { computed, onMounted, ref, watch } from 'vue'
import draggable from 'vuedraggable'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import { useMembers } from '@/composables/useMembers'
import { EventsApi } from '@/features/events/events.api'
import { ROLE } from '@/features/events/events.types'

const emit = defineEmits<{
  eventPlanned: []
}>()

const modelValue = defineModel<boolean>('isOpen', { required: true })
const eventValue = defineModel<FCEvent>('fcEvent', { required: true })

const { members, load: memberLoad } = useMembers()
const saving = ref(false)

// Group management state
const groups = ref<EventGroupRequest[]>([])

// Role picker state
const rolePickerOpen = ref(false)
const rolePickerTarget = ref<{
  groupIndex: number
  signup: EventSignup
  element: HTMLElement | null
} | null>(null)

// Info/confirmation modals
const isInfoOpen = ref(false)
const infoMessage = ref('')
const isInsufficientParticipantsOpen = ref(false)

// Manual signup state
const manualSearchQuery = ref('')
const manualRolePickerOpen = ref(false)
const manualRolePickerMember = ref<{ DiscordId: string, DiscordName: string, PlayerName: string } | null>(null)
const manualSelectedRoles = ref<Role[]>([])
const manualSelectedSlugs = ref<string[]>([])

interface UnassignedItem {
  DiscordUserId: string
  Roles: Role[]
  SignupSlugs?: string[]
}

const usesCustomButtons = computed(() =>
  eventValue.value.SignupButtonConfigs && eventValue.value.SignupButtonConfigs.length > 0,
)

function isHelperSignup(signup: EventSignup | UnassignedItem): boolean {
  if (!usesCustomButtons.value || !eventValue.value.SignupButtonConfigs) return false
  const slugs = 'SignupSlugs' in signup ? signup.SignupSlugs : (signup as EventSignup).SignupSlugs
  if (!slugs) return false
  return slugs.some(slug =>
    eventValue.value.SignupButtonConfigs!.find(c => c.Slug === slug)?.IsHelper === true,
  )
}

function getSlugLabels(slugs?: string[]): string[] {
  if (!slugs || !eventValue.value.SignupButtonConfigs) return []
  return slugs
    .map(slug => eventValue.value.SignupButtonConfigs!.find(c => c.Slug === slug)?.Label ?? slug)
}

// Compute unassigned signups (not yet placed in any group)
const unassigned = computed((): UnassignedItem[] => {
  const assignedIds = new Set(
    groups.value.flatMap(g => g.Participants.map(p => p.DiscordUserId)),
  )

  if (usesCustomButtons.value) {
    return (eventValue.value.Signups ?? [])
      .filter(s => !assignedIds.has(s.DiscordUserId) && s.SignupSlugs && s.SignupSlugs.length > 0)
      .map(s => ({ DiscordUserId: s.DiscordUserId, Roles: s.Roles ?? [], SignupSlugs: s.SignupSlugs }))
  }

  return (eventValue.value.Signups ?? [])
    .filter(s => !assignedIds.has(s.DiscordUserId) && s.Roles.length > 0)
    .map(s => ({ DiscordUserId: s.DiscordUserId, Roles: [...s.Roles] }))
})

const uniqueSignupCount = computed(() => {
  if (usesCustomButtons.value) {
    return (eventValue.value.Signups ?? []).filter(s => s.SignupSlugs && s.SignupSlugs.length > 0).length
  }
  return (eventValue.value.Signups ?? []).filter(s => s.Roles.length > 0).length
})

const requiredParticipants = computed(() => eventValue.value.RequiredParticipants ?? eventValue.value.MaxNumberOfParticipants)
const groupSize = computed(() => Math.min(requiredParticipants.value || 8, 8))
const numberOfGroups = computed(() => requiredParticipants.value === 0 ? 0 : Math.ceil(requiredParticipants.value / groupSize.value))

const canAddGroup = computed(() => {
  if (numberOfGroups.value === 0) {
    return unassigned.value.length > 0
  }
  return uniqueSignupCount.value >= (groups.value.length + 1) * groupSize.value
})

const signupsNeededForNextGroup = computed(() => {
  if (numberOfGroups.value === 0) return 0
  const needed = (groups.value.length + 1) * groupSize.value
  return Math.max(0, needed - uniqueSignupCount.value)
})

const totalAssigned = computed(() => {
  return groups.value.reduce((sum, g) => sum + g.Participants.length, 0)
})

const manualSearchResults = computed(() => {
  const query = manualSearchQuery.value.trim().toLowerCase()
  if (!query)
    return []

  const signedUpIds = new Set(
    (eventValue.value.Signups ?? []).map(s => s.DiscordUserId),
  )

  return members.value
    .filter((m) => {
      if (signedUpIds.has(m.DiscordId))
        return false
      const playerName = (m.PlayerName || '').toLowerCase()
      const discordName = (m.DiscordName || '').toLowerCase()
      return playerName.includes(query) || discordName.includes(query)
    })
    .slice(0, 10)
})

function getMemberName(discordId: string): string {
  const member = members.value.find(m => m.DiscordId === discordId)
  return member?.PlayerName || member?.DiscordName || discordId
}

function getMemberAvatar(discordId: string): string | null {
  const member = members.value.find(m => m.DiscordId === discordId)
  return member?.DiscordAvatar || null
}

function roleLabel(role: Role): string {
  const labels: Record<number, string> = {
    [ROLE.Tank]: 'Tank',
    [ROLE.Healer]: 'Healer',
    [ROLE.Melee]: 'Melee',
    [ROLE.Caster]: 'Caster',
    [ROLE.Ranged]: 'Ranged',
  }
  return labels[role] ?? 'Unknown'
}

function roleShort(role: Role): string {
  const labels: Record<number, string> = {
    [ROLE.Tank]: 'T',
    [ROLE.Healer]: 'H',
    [ROLE.Melee]: 'M',
    [ROLE.Caster]: 'C',
    [ROLE.Ranged]: 'R',
  }
  return labels[role] ?? '?'
}

function getSignupRoles(discordUserId: string): Role[] {
  const signup = eventValue.value.Signups?.find(s => s.DiscordUserId === discordUserId)
  if (!signup) return []

  if (!usesCustomButtons.value) return signup.Roles ?? []

  const configs = eventValue.value.SignupButtonConfigs!
  const roles: Role[] = []
  for (const slug of signup.SignupSlugs ?? []) {
    const config = configs.find(c => c.Slug === slug)
    if (config?.MappedRole !== undefined) {
      roles.push(config.MappedRole)
    }
  }
  return roles
}

// Initialize from existing groups or empty
function initGroups() {
  if (eventValue.value.Groups && eventValue.value.Groups.length > 0) {
    groups.value = eventValue.value.Groups.map(g => ({
      Id: g.Id,
      Name: g.Name,
      Participants: [...g.Participants],
    }))
  }
  else {
    groups.value = []
  }
}

function addGroup() {
  if (!canAddGroup.value) {
    openInfo(`Need at least ${(groups.value.length + 1) * groupSize.value} unique signups to create ${groups.value.length + 1} group(s). Currently have ${uniqueSignupCount.value}.`)
    return
  }
  groups.value.push({
    Name: `Group ${groups.value.length + 1}`,
    Participants: [],
  })
}

function removeGroup(index: number) {
  groups.value.splice(index, 1)
}

// Unified handler for all drops into a group
function onGroupChange(groupIndex: number, evt: any) {
  if (!evt.added) return

  const addedItem = evt.added.element
  const addedIndex = evt.added.newIndex
  const group = groups.value[groupIndex]

  // Capacity check
  if (group.Participants.length > groupSize.value) {
    group.Participants.splice(addedIndex, 1)
    openInfo(`This group already has ${groupSize.value} participants (maximum).`)
    return
  }

  // If the item already has a Role (inter-group move), keep it as-is
  if (addedItem.Role !== undefined) return

  // Item came from the pool (UnassignedItem) — remove it, then assign a role
  group.Participants.splice(addedIndex, 1)

  const signupRoles = getSignupRoles(addedItem.DiscordUserId)
  if (signupRoles.length === 0 && usesCustomButtons.value) {
    rolePickerTarget.value = {
      groupIndex,
      signup: {
        DiscordUserId: addedItem.DiscordUserId,
        Roles: [ROLE.Tank, ROLE.Healer, ROLE.Melee, ROLE.Caster, ROLE.Ranged],
        SignupDate: new Date(),
      },
      element: null,
    }
    rolePickerOpen.value = true
    return
  }
  if (signupRoles.length === 1) {
    group.Participants.splice(addedIndex, 0, {
      DiscordUserId: addedItem.DiscordUserId,
      Role: signupRoles[0],
      SelectionDate: new Date(),
    })
  }
  else if (signupRoles.length > 1) {
    rolePickerTarget.value = {
      groupIndex,
      signup: {
        DiscordUserId: addedItem.DiscordUserId,
        Roles: signupRoles,
        SignupDate: new Date(),
      },
      element: null,
    }
    rolePickerOpen.value = true
  }
}

function selectRoleForDrop(role: Role) {
  if (!rolePickerTarget.value) return

  const { groupIndex, signup } = rolePickerTarget.value
  const group = groups.value[groupIndex]

  group.Participants.push({
    DiscordUserId: signup.DiscordUserId,
    Role: role,
    SelectionDate: new Date(),
  })

  rolePickerOpen.value = false
  rolePickerTarget.value = null
}

function cancelRolePicker() {
  rolePickerOpen.value = false
  rolePickerTarget.value = null
}

function openManualRolePicker(member: { DiscordId: string, DiscordName: string, PlayerName: string }) {
  manualRolePickerMember.value = member
  manualSelectedRoles.value = []
  manualSelectedSlugs.value = []
  manualRolePickerOpen.value = true
  manualSearchQuery.value = ''
}

function toggleManualRole(role: Role) {
  const idx = manualSelectedRoles.value.indexOf(role)
  if (idx >= 0) {
    manualSelectedRoles.value.splice(idx, 1)
  }
  else {
    manualSelectedRoles.value.push(role)
  }
}

function toggleManualSlug(slug: string) {
  const idx = manualSelectedSlugs.value.indexOf(slug)
  if (idx >= 0) {
    manualSelectedSlugs.value.splice(idx, 1)
  }
  else {
    manualSelectedSlugs.value.push(slug)
  }
}

async function confirmManualSignup() {
  if (!manualRolePickerMember.value) return

  const isCustom = usesCustomButtons.value
  if (isCustom && manualSelectedSlugs.value.length === 0) return
  if (!isCustom && manualSelectedRoles.value.length === 0) return

  try {
    if (isCustom) {
      await EventsApi.manualSignupWithSlugs(
        eventValue.value.Id,
        manualRolePickerMember.value.DiscordId,
        [...manualSelectedSlugs.value],
      )
    }
    else {
      await EventsApi.manualSignup(
        eventValue.value.Id,
        manualRolePickerMember.value.DiscordId,
        [...manualSelectedRoles.value],
      )
    }

    if (!eventValue.value.Signups) {
      eventValue.value.Signups = []
    }

    eventValue.value.Signups.push({
      DiscordUserId: manualRolePickerMember.value.DiscordId,
      Roles: isCustom ? [] : [...manualSelectedRoles.value],
      SignupSlugs: isCustom ? [...manualSelectedSlugs.value] : undefined,
      SignupDate: new Date(),
    })

    manualRolePickerOpen.value = false
    manualRolePickerMember.value = null
    manualSelectedRoles.value = []
    manualSelectedSlugs.value = []
  }
  catch (error) {
    console.error('Error adding manual signup:', error)
    openInfo('Failed to add member. Please try again.')
    manualRolePickerOpen.value = false
    manualRolePickerMember.value = null
    manualSelectedRoles.value = []
    manualSelectedSlugs.value = []
  }
}

function cancelManualRolePicker() {
  manualRolePickerOpen.value = false
  manualRolePickerMember.value = null
  manualSelectedRoles.value = []
  manualSelectedSlugs.value = []
}

function onManualSearchKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter' && manualSearchResults.value.length > 0) {
    const first = manualSearchResults.value[0]
    openManualRolePicker({ DiscordId: first.DiscordId, DiscordName: first.DiscordName, PlayerName: first.PlayerName })
  }
}

// Remove participant from a group back to pool
function removeFromGroup(groupIndex: number, participantIndex: number) {
  groups.value[groupIndex].Participants.splice(participantIndex, 1)
}

function openInfo(message: string) {
  infoMessage.value = message
  isInfoOpen.value = true
}

function handleSave() {
  if (groups.value.length === 0) {
    openInfo('Please create at least one group and assign participants.')
    return
  }

  const allFull = groups.value.every(g => g.Participants.length === groupSize.value)
  if (!allFull) {
    isInsufficientParticipantsOpen.value = true
    return
  }

  doSave()
}

async function doSave() {
  isInsufficientParticipantsOpen.value = false
  saving.value = true
  try {
    await EventsApi.selectParticipants(eventValue.value.Id, groups.value)
    modelValue.value = false
    emit('eventPlanned')
  }
  catch (error) {
    console.error('Error saving groups:', error)
    openInfo('Failed to save groups. Please try again.')
  }
  finally {
    saving.value = false
  }
}

// Load members when component mounts
onMounted(() => {
  if (members.value.length === 0) {
    memberLoad()
  }
  initGroups()
})

// Reinitialize when dialog opens
watch(modelValue, (isOpen) => {
  if (isOpen) {
    initGroups()
    manualSearchQuery.value = ''
  }
})
</script>

<template>
  <BaseModal v-model="modelValue" title="Organize Event" :close-on-outside-click="false" size="large">
    <template #image>
      <img v-if="eventValue.PictureUrl" :src="eventValue.PictureUrl" alt="avatar" class="card__image">
    </template>
    <template #body>
      <p>
        Assign signups to groups for '<b>{{ eventValue.Name }}</b>'.
        Each group can have up to <b>{{ groupSize }}</b> participants.
        Drag members from the pool into groups.
      </p>
      <p class="muted" style="font-size: 0.9rem; margin-bottom: 1rem;">
        The bot will automatically post a new message in <b>#upcoming-roster</b>.
      </p>

      <!-- Summary -->
      <div class="org-summary">
        <span>Signups: <b>{{ uniqueSignupCount }}</b></span>
        <span>Groups: <b>{{ groups.length }}</b></span>
        <span>Assigned: <b>{{ totalAssigned }}</b></span>
        <span>Unassigned: <b>{{ unassigned.length }}</b></span>
      </div>

      <div class="org-layout">
        <!-- Left: Unassigned pool -->
        <div class="pool-panel">
          <h4 class="panel-title">Unassigned Pool ({{ unassigned.length }})</h4>
          <draggable
            :list="unassigned"
            :group="{ name: 'group-drop', pull: 'clone', put: false }"
            item-key="DiscordUserId"
            class="pool-list"
            :clone="(item: UnassignedItem) => ({ ...item })"
            :sort="false"
          >
            <template #item="{ element }">
              <div class="pool-item">
                <img
                  v-if="getMemberAvatar(element.DiscordUserId)"
                  :src="getMemberAvatar(element.DiscordUserId)!"
                  alt="avatar"
                  class="avatar"
                >
                <div v-else class="avatar placeholder">
                  {{ getMemberName(element.DiscordUserId).charAt(0).toUpperCase() }}
                </div>
                <div class="pool-item-info">
                  <span class="member-name">{{ getMemberName(element.DiscordUserId) }}</span>
                  <span v-if="usesCustomButtons" class="role-tags">
                    <span
                      v-for="label in getSlugLabels(element.SignupSlugs)"
                      :key="label"
                      class="role-tag"
                    >{{ label }}</span>
                  </span>
                  <span v-else class="role-tags">
                    <span
                      v-for="role in element.Roles"
                      :key="role"
                      class="role-tag"
                    >{{ roleShort(role) }}</span>
                  </span>
                  <span v-if="isHelperSignup(element)" class="helper-badge">Helper</span>
                </div>
              </div>
            </template>
          </draggable>
          <div v-if="unassigned.length === 0" class="empty-pool">
            All signups assigned
          </div>

          <!-- Manual member add -->
          <div class="manual-add-section">
            <label class="manual-add-label">Add member manually:</label>
            <div class="manual-add-wrapper">
              <input
                v-model="manualSearchQuery"
                class="manual-add-input"
                placeholder="Search by name..."
                type="text"
                @keydown="onManualSearchKeydown"
              >
              <div v-if="manualSearchResults.length > 0" class="manual-add-dropdown">
                <div
                  v-for="member in manualSearchResults"
                  :key="member.DiscordId"
                  class="manual-add-result"
                  @click="openManualRolePicker(member)"
                >
                  <img
                    v-if="member.DiscordAvatar"
                    :src="member.DiscordAvatar"
                    alt="avatar"
                    class="avatar-sm"
                  >
                  <div v-else class="avatar-sm placeholder">
                    {{ (member.PlayerName || member.DiscordName).charAt(0).toUpperCase() }}
                  </div>
                  <div class="manual-add-result-info">
                    <span class="member-name">{{ member.PlayerName || member.DiscordName }}</span>
                    <span v-if="member.PlayerName && member.DiscordName" class="manual-add-discord-name">
                      {{ member.DiscordName }}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right: Groups -->
        <div class="groups-panel">
          <div v-for="(group, gIdx) in groups" :key="gIdx" class="group-card">
            <div class="group-header">
              <input
                v-model="group.Name"
                class="group-name-input"
                type="text"
                placeholder="Group name..."
              >
              <span class="group-count">{{ group.Participants.length }}/{{ groupSize }}</span>
              <button class="btn-remove-group" title="Remove group" @click="removeGroup(gIdx)">
                &times;
              </button>
            </div>

            <draggable
              :list="group.Participants"
              :group="{ name: 'group-drop', put: true }"
              item-key="DiscordUserId"
              class="group-list"
              @change="onGroupChange(gIdx, $event)"
            >
              <template #item="{ element, index }">
                <div class="group-member">
                  <img
                    v-if="getMemberAvatar(element.DiscordUserId)"
                    :src="getMemberAvatar(element.DiscordUserId)!"
                    alt="avatar"
                    class="avatar-sm"
                  >
                  <div v-else class="avatar-sm placeholder">
                    {{ getMemberName(element.DiscordUserId).charAt(0).toUpperCase() }}
                  </div>
                  <span class="member-name">{{ getMemberName(element.DiscordUserId) }}</span>
                  <span class="assigned-role">{{ roleLabel(element.Role) }}</span>
                  <button class="btn-remove-member" title="Remove" @click="removeFromGroup(gIdx, index)">
                    &times;
                  </button>
                </div>
              </template>
              <template #footer>
                <div v-if="group.Participants.length < groupSize" class="dropzone-hint">
                  Drop here to add
                </div>
              </template>
            </draggable>
          </div>

          <BaseButton
            :disabled="!canAddGroup"
            :title="`+ Add Group`"
            :tooltip="canAddGroup ? 'Create a new group' : `Need ${(groups.length + 1) * groupSize} signups to add another group`"
            size="small"
            state="secondary"
            @clicked="addGroup"
          />
          <div v-if="!canAddGroup" class="not-enough-signups">
            <template v-if="groups.length === 0">
              Need <b>{{ signupsNeededForNextGroup }}</b> more signup{{ signupsNeededForNextGroup === 1 ? '' : 's' }}
              to create a group ({{ uniqueSignupCount }}/{{ groupSize }} required).
            </template>
            <template v-else>
              Need <b>{{ signupsNeededForNextGroup }}</b> more signup{{ signupsNeededForNextGroup === 1 ? '' : 's' }}
              to add another group.
            </template>
          </div>
        </div>
      </div>
    </template>

    <template #actions>
      <BaseButton :disabled="saving" state="secondary" title="Cancel" @clicked="modelValue = false" />
      <BaseButton :disabled="saving || groups.length === 0" :title="saving ? 'Saving...' : 'Save'" state="primary" @click="handleSave" />
    </template>
  </BaseModal>

  <!-- Role Picker Modal -->
  <BaseModal
    v-model="rolePickerOpen"
    :close-on-outside-click="false"
    size="small"
    title="Select Role"
  >
    <template #body>
      <p v-if="rolePickerTarget">
        Assign a role for <b>{{ getMemberName(rolePickerTarget.signup.DiscordUserId) }}</b>:
      </p>
      <div v-if="rolePickerTarget" class="role-picker-options">
        <button
          v-for="role in rolePickerTarget.signup.Roles"
          :key="role"
          class="role-picker-btn"
          @click="selectRoleForDrop(role)"
        >
          {{ roleLabel(role) }}
        </button>
      </div>
    </template>
    <template #actions>
      <BaseButton state="secondary" title="Cancel" @clicked="cancelRolePicker" />
    </template>
  </BaseModal>

  <!-- Manual Signup Role Picker Modal -->
  <BaseModal
    v-model="manualRolePickerOpen"
    :close-on-outside-click="false"
    size="small"
    :title="usesCustomButtons ? 'Select Signups' : 'Select Roles'"
  >
    <template #body>
      <p v-if="manualRolePickerMember">
        Select {{ usesCustomButtons ? 'signup(s)' : 'role(s)' }} for <b>{{ manualRolePickerMember.PlayerName || manualRolePickerMember.DiscordName }}</b>:
      </p>

      <!-- Custom button slug picker -->
      <div v-if="usesCustomButtons && eventValue.SignupButtonConfigs" class="role-picker-options">
        <button
          v-for="config in eventValue.SignupButtonConfigs"
          :key="config.Slug"
          class="role-picker-btn"
          :class="{ 'role-picker-btn--selected': manualSelectedSlugs.includes(config.Slug) }"
          @click="toggleManualSlug(config.Slug)"
        >
          {{ config.Label }}
        </button>
      </div>

      <!-- Legacy role picker -->
      <div v-else class="role-picker-options">
        <button
          v-for="role in [ROLE.Tank, ROLE.Healer, ROLE.Melee, ROLE.Caster, ROLE.Ranged]"
          :key="role"
          class="role-picker-btn"
          :class="{ 'role-picker-btn--selected': manualSelectedRoles.includes(role) }"
          @click="toggleManualRole(role)"
        >
          {{ roleLabel(role) }}
        </button>
      </div>

      <p v-if="(usesCustomButtons ? manualSelectedSlugs.length : manualSelectedRoles.length) === 0" class="manual-role-hint">
        Select at least one {{ usesCustomButtons ? 'signup' : 'role' }}.
      </p>
    </template>
    <template #actions>
      <BaseButton state="secondary" title="Cancel" @clicked="cancelManualRolePicker" />
      <BaseButton
        :disabled="usesCustomButtons ? manualSelectedSlugs.length === 0 : manualSelectedRoles.length === 0"
        state="primary"
        title="Add to Pool"
        @clicked="confirmManualSignup"
      />
    </template>
  </BaseModal>

  <!-- Info modal -->
  <BaseModal
    v-model="isInfoOpen"
    :close-on-outside-click="true"
    size="small"
    title="Notice"
  >
    <template #body>
      <p>{{ infoMessage }}</p>
    </template>
    <template #actions>
      <BaseButton state="primary" title="OK" @clicked="isInfoOpen = false" />
    </template>
  </BaseModal>

  <!-- Insufficient participants confirmation modal -->
  <BaseModal
    v-model="isInsufficientParticipantsOpen"
    :close-on-outside-click="false"
    size="small"
    title="Incomplete Groups"
  >
    <template #body>
      <div class="insufficient-warning">
        <p>
          Not all groups are full. Some groups have fewer than
          <strong>{{ groupSize }}</strong> participants.
        </p>
        <p class="warning-question">
          Do you want to save anyway?
        </p>
      </div>
    </template>
    <template #actions>
      <BaseButton state="secondary" title="Go Back" @clicked="isInsufficientParticipantsOpen = false" />
      <BaseButton state="warning" title="Save Anyway" @clicked="doSave" />
    </template>
  </BaseModal>
</template>

<style scoped>
.card__image {
  transform: scale(1.1);
}

.org-summary {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  padding: 0.75rem 1rem;
  background: var(--muted-bg);
  border-radius: 8px;
  border: 1px solid var(--border);
  margin-bottom: 1rem;
  font-size: 0.9rem;
}

.org-layout {
  display: flex;
  gap: 1rem;
  min-height: 300px;
}

.pool-panel {
  flex: 0 0 280px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  display: flex;
  flex-direction: column;
}

.groups-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.panel-title {
  margin: 0;
  padding: 0.75rem 1rem;
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--fg);
  border-bottom: 1px solid var(--border);
}

.pool-list {
  flex: 1;
  overflow-y: auto;
  max-height: 500px;
  min-height: 100px;
}

.pool-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border-bottom: 1px solid var(--border);
  cursor: grab;
  transition: background 0.2s;
}

.pool-item:hover {
  background: var(--muted-bg);
}

.pool-item:active {
  cursor: grabbing;
}

.pool-item-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.role-tags {
  display: flex;
  gap: 3px;
}

.role-tag {
  font-size: 0.7rem;
  font-weight: 700;
  padding: 1px 5px;
  border-radius: 8px;
  background: var(--muted-bg);
  color: var(--muted);
  border: 1px solid var(--border);
}

.helper-badge {
  font-size: 0.65rem;
  font-weight: 700;
  padding: 1px 5px;
  border-radius: 8px;
  background: #fef3c7;
  color: #92400e;
  border: 1px solid #fcd34d;
}

.not-enough-signups {
  padding: 0.6rem 0.75rem;
  font-size: 0.85rem;
  color: var(--muted);
  background: var(--muted-bg);
  border: 1px solid var(--border);
  border-radius: 8px;
}

.empty-pool {
  padding: 2rem;
  text-align: center;
  color: var(--muted);
  font-style: italic;
}

.group-card {
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  overflow: hidden;
}

.group-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  background: var(--muted-bg);
  border-bottom: 1px solid var(--border);
}

.group-name-input {
  flex: 1;
  padding: 0.35rem 0.5rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  color: var(--fg);
  font-weight: 600;
  font-size: 0.9rem;
}

.group-count {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--muted);
  white-space: nowrap;
}

.btn-remove-group {
  background: none;
  border: none;
  font-size: 1.3rem;
  cursor: pointer;
  color: var(--muted);
  line-height: 1;
  padding: 0 4px;
}

.btn-remove-group:hover {
  color: var(--danger);
}

.group-list {
  min-height: 40px;
}

.group-member {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.4rem 0.75rem;
  border-bottom: 1px solid var(--border);
  cursor: grab;
  transition: background 0.2s;
}

.group-member:hover {
  background: var(--muted-bg);
}

.group-member:active {
  cursor: grabbing;
}

.assigned-role {
  margin-left: auto;
  font-size: 0.8rem;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 8px;
  background: var(--link);
  color: var(--bg);
}

.btn-remove-member {
  background: none;
  border: none;
  font-size: 1.1rem;
  cursor: pointer;
  color: var(--muted);
  line-height: 1;
  padding: 0 2px;
  flex-shrink: 0;
}

.btn-remove-member:hover {
  color: var(--danger);
}

.dropzone-hint {
  padding: 0.5rem;
  text-align: center;
  color: var(--muted);
  font-size: 0.8rem;
  font-style: italic;
  border: 2px dashed var(--border);
  border-radius: 8px;
  margin: 0.5rem;
}

.avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.avatar.placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--muted-bg);
  color: var(--muted);
  font-weight: 700;
  font-size: 0.85rem;
  border: 1px solid var(--border);
}

.avatar-sm {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}

.avatar-sm.placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--muted-bg);
  color: var(--muted);
  font-weight: 700;
  font-size: 0.7rem;
  border: 1px solid var(--border);
}

.member-name {
  font-weight: 500;
  color: var(--fg);
  font-size: 0.9rem;
}

/* Role picker */
.role-picker-options {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.75rem;
}

.role-picker-btn {
  padding: 0.6rem 1.2rem;
  border: 2px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  color: var(--fg);
  font-weight: 600;
  font-size: 0.95rem;
  cursor: pointer;
  transition: all 0.2s;
}

.role-picker-btn:hover {
  border-color: var(--link);
  background: var(--muted-bg);
  color: var(--link);
}

/* Insufficient participants warning */
.insufficient-warning {
  text-align: center;
}

.insufficient-warning p {
  margin: 0 0 1rem 0;
  color: var(--fg);
  font-size: 1rem;
  line-height: 1.5;
}

.insufficient-warning p:last-child {
  margin-bottom: 0;
}

.insufficient-warning .warning-question {
  color: var(--muted);
  font-size: 0.95rem;
}

/* Scrollbar styling */
.pool-list::-webkit-scrollbar {
  width: 8px;
}

.pool-list::-webkit-scrollbar-track {
  background: var(--muted-bg);
  border-radius: 8px;
}

.pool-list::-webkit-scrollbar-thumb {
  background: var(--border);
  border-radius: 8px;
}

.pool-list::-webkit-scrollbar-thumb:hover {
  background: var(--muted);
}

/* Responsive */
@media (max-width: 768px) {
  .org-layout {
    flex-direction: column;
  }

  .pool-panel {
    flex: none;
  }

  .pool-list {
    max-height: 200px;
  }
}

.muted {
  color: var(--muted);
}

/* Drag ghost styling */
.sortable-ghost {
  opacity: 0.5;
  background: var(--muted-bg);
}

.sortable-chosen {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

/* Manual add section */
.manual-add-section {
  padding: 0.75rem;
  border-top: 1px solid var(--border);
}

.manual-add-label {
  display: block;
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--muted);
  margin-bottom: 0.4rem;
}

.manual-add-wrapper {
  position: relative;
}

.manual-add-input {
  width: 100%;
  padding: 0.5rem 0.6rem;
  font-size: 0.85rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--card);
  color: var(--fg);
  outline: none;
  transition: border-color 0.2s;
  box-sizing: border-box;
}

.manual-add-input:focus {
  border-color: var(--link);
}

.manual-add-dropdown {
  position: absolute;
  bottom: 100%;
  left: 0;
  right: 0;
  background: var(--card);
  border: 1px solid var(--border);
  border-radius: 8px;
  margin-bottom: 4px;
  max-height: 240px;
  overflow-y: auto;
  box-shadow: 0 -4px 12px rgba(0, 0, 0, 0.12);
  z-index: 1000;
}

.manual-add-result {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.45rem 0.6rem;
  cursor: pointer;
  font-size: 0.85rem;
  transition: background 0.2s;
}

.manual-add-result:hover {
  background: var(--muted-bg);
}

.manual-add-result:not(:last-child) {
  border-bottom: 1px solid var(--border);
}

.manual-add-result-info {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.manual-add-discord-name {
  font-size: 0.75rem;
  color: var(--muted);
}

.manual-role-hint {
  font-size: 0.85rem;
  color: var(--muted);
  font-style: italic;
  margin-top: 0.5rem;
}

.role-picker-btn--selected {
  border-color: var(--link);
  background: var(--link);
  color: var(--bg);
}

.role-picker-btn--selected:hover {
  opacity: 0.9;
  border-color: var(--link);
  background: var(--link);
  color: var(--bg);
}
</style>
