<script setup lang="ts">
import type { EventOccurrence, FCEvent, Role } from '@/features/events/events.types'
import { computed, onMounted, ref, watch } from 'vue'

import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import DiscordMessageRenderer from '@/components/DiscordMessageRenderer.vue'
import { useAuth } from '@/composables/useAuth'
import { useEvents } from '@/composables/useEvents'
import { useMembers } from '@/composables/useMembers'
import { ROLE } from '@/features/events/events.types'

const props = defineProps<{
  modelValue: boolean
  event: FCEvent
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()

const { user } = useAuth()
const members = useMembers()
const eventsComposable = useEvents()
const fcEvent = ref<FCEvent | null>(props.event)

// Get next occurrence for signup
const nextOccurrence = computed((): EventOccurrence | null => {
  if (!fcEvent.value)
    return null
  return eventsComposable.getNextOccurrence(fcEvent.value)
})

// Watch for event prop changes
watch(() => props.event, (newEvent) => {
  fcEvent.value = newEvent
}, { deep: true })

// Load members data when component mounts
onMounted(() => {
  if (members.members.value.length === 0) {
    members.load()
  }
})

// Check if current user is signed up for a specific role (in next occurrence)
function isSignedUpForRole(role: Role): boolean {
  if (!user.value?.DiscordId || !nextOccurrence.value)
    return false

  return nextOccurrence.value.Signups?.some(signup =>
    signup.DiscordUserId === user.value?.DiscordId && signup.Roles.includes(role),
  ) ?? false
}

// Get signup count for a specific role (in next occurrence)
function getSignupCountForRole(role: Role): number {
  if (!nextOccurrence.value?.Signups)
    return 0

  return nextOccurrence.value.Signups.filter(signup =>
    signup.Roles.includes(role),
  ).length
}

// Get list of user display names signed up for a specific role (in next occurrence)
function getSignedUpUsersForRole(role: Role): string[] {
  if (!nextOccurrence.value?.Signups)
    return []

  return nextOccurrence.value.Signups
    .filter(signup => signup.Roles.includes(role))
    .map((signup) => {
      const member = members.members.value.find(m => m.DiscordId === signup.DiscordUserId)

      if (!member)
        return signup.DiscordUserId // Fallback to ID if member not found

      // Return PlayerName if present, otherwise DiscordName
      return member.PlayerName || member.DiscordName
    })
}

// Computed properties for each role
const isSignedUpTank = computed(() => isSignedUpForRole(ROLE.Tank))
const isSignedUpHealer = computed(() => isSignedUpForRole(ROLE.Healer))
const isSignedUpMelee = computed(() => isSignedUpForRole(ROLE.Melee))
const isSignedUpCaster = computed(() => isSignedUpForRole(ROLE.Caster))
const isSignedUpRanged = computed(() => isSignedUpForRole(ROLE.Ranged))

// Signup counts for each role
const tankCount = computed(() => getSignupCountForRole(ROLE.Tank))
const healerCount = computed(() => getSignupCountForRole(ROLE.Healer))
const meleeCount = computed(() => getSignupCountForRole(ROLE.Melee))
const casterCount = computed(() => getSignupCountForRole(ROLE.Caster))
const rangedCount = computed(() => getSignupCountForRole(ROLE.Ranged))

// Signed up users for tooltips
const tankUsers = computed(() => getSignedUpUsersForRole(ROLE.Tank))
const healerUsers = computed(() => getSignedUpUsersForRole(ROLE.Healer))
const meleeUsers = computed(() => getSignedUpUsersForRole(ROLE.Melee))
const casterUsers = computed(() => getSignedUpUsersForRole(ROLE.Caster))
const rangedUsers = computed(() => getSignedUpUsersForRole(ROLE.Ranged))

async function signUp(signupEvent: FCEvent, role: Role) {
  if (!nextOccurrence.value) {
    alert('No upcoming occurrence available for signup')
    return
  }

  // Get current roles user is signed up for
  const currentSignup = nextOccurrence.value.Signups?.find(s => s.DiscordUserId === user.value?.DiscordId)
  const currentRoles = currentSignup?.Roles ?? []

  // Toggle the role: if already signed up for this role, remove it; otherwise add it
  let updatedRoles: Role[]
  if (currentRoles.includes(role)) {
    updatedRoles = currentRoles.filter(r => r !== role)
  }
  else {
    updatedRoles = [...currentRoles, role]
  }

  try {
    await eventsComposable.signUpForOccurrence(signupEvent.Id, nextOccurrence.value.Id, updatedRoles)
    // Reload event after signup and update button states
    fcEvent.value = await eventsComposable.getEvent(signupEvent.Id)
  }
  catch (error) {
    console.error('Error signing up:', error)
    alert('Error signing up. Please try again.')
  }
}
</script>

<template>
  <BaseModal
    :model-value="props.modelValue" :title="`${event.Name} - signup`"
    :description="event.Description" @update:model-value="emit('update:modelValue', $event)"
  >
    <template #body>
      <DiscordMessageRenderer :content="event.Description" />
      <div v-if="nextOccurrence" class="occurrence-info">
        <strong>Signing up for:</strong>
        {{
          new Date(nextOccurrence.OccurrenceDate).toLocaleString(undefined, {
            weekday: 'short',
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
          })
        }}
      </div>
      <div v-else class="no-occurrence-warning">
        No upcoming occurrences available for signup.
      </div>
    </template>
    <template #image>
      <img v-if="event.PictureUrl" :src="event.PictureUrl" alt="avatar" class="card__image">
    </template>

    <template #actions>
      <BaseButton
        :title="`Tank (${tankCount})`"
        :tooltip="tankUsers.length > 0 ? tankUsers.join(', ') : 'No signups yet'"
        :state="isSignedUpTank ? 'pressed' : 'primary'"
        @clicked="signUp(event, ROLE.Tank)"
      />
      <BaseButton
        :title="`Healer (${healerCount})`"
        :tooltip="healerUsers.length > 0 ? healerUsers.join(', ') : 'No signups yet'"
        :state="isSignedUpHealer ? 'pressed' : 'primary'"
        @clicked="signUp(event, ROLE.Healer)"
      />
      <BaseButton
        :title="`Melee (${meleeCount})`"
        :tooltip="meleeUsers.length > 0 ? meleeUsers.join(', ') : 'No signups yet'"
        :state="isSignedUpMelee ? 'pressed' : 'primary'"
        @clicked="signUp(event, ROLE.Melee)"
      />
      <BaseButton
        :title="`Caster (${casterCount})`"
        :tooltip="casterUsers.length > 0 ? casterUsers.join(', ') : 'No signups yet'"
        :state="isSignedUpCaster ? 'pressed' : 'primary'"
        @clicked="signUp(event, ROLE.Caster)"
      />
      <BaseButton
        :title="`Ranged (${rangedCount})`"
        :tooltip="rangedUsers.length > 0 ? rangedUsers.join(', ') : 'No signups yet'"
        :state="isSignedUpRanged ? 'pressed' : 'primary'"
        @clicked="signUp(event, ROLE.Ranged)"
      />
    </template>
  </BaseModal>
</template>

<style scoped>
.occurrence-info {
  margin-top: 16px;
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

.no-occurrence-warning {
  margin-top: 16px;
  padding: 12px;
  background: #fff3cd;
  border: 1px solid #ffc107;
  border-radius: 8px;
  color: #856404;
  font-weight: 500;
}
</style>
