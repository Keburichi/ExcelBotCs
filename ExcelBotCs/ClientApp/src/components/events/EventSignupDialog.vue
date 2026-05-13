<script setup lang="ts">
import type { FCEvent, Role } from '@/features/events/events.types'
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

// Check if current user is signed up for a specific role
function isSignedUpForRole(role: Role): boolean {
  if (!user.value?.DiscordId || !fcEvent.value?.Signups)
    return false

  return fcEvent.value.Signups.some(signup =>
    signup.DiscordUserId === user.value?.DiscordId && signup.Roles.includes(role),
  )
}

// Get signup count for a specific role
function getSignupCountForRole(role: Role): number {
  if (!fcEvent.value?.Signups)
    return 0

  return fcEvent.value.Signups.filter(signup =>
    signup.Roles.includes(role),
  ).length
}

// Get list of user display names signed up for a specific role
function getSignedUpUsersForRole(role: Role): string[] {
  if (!fcEvent.value?.Signups)
    return []

  return fcEvent.value.Signups
    .filter(signup => signup.Roles.includes(role))
    .map((signup) => {
      const member = members.members.value.find(m => m.DiscordId === signup.DiscordUserId)

      if (!member)
        return signup.DiscordUserId

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
  // Get current roles user is signed up for
  const currentSignup = fcEvent.value?.Signups?.find(s => s.DiscordUserId === user.value?.DiscordId)
  const currentRoles = currentSignup?.Roles ?? []

  // Toggle the role
  let updatedRoles: Role[]
  if (currentRoles.includes(role)) {
    updatedRoles = currentRoles.filter(r => r !== role)
  }
  else {
    updatedRoles = [...currentRoles, role]
  }

  try {
    if (updatedRoles.length === 0 && currentSignup) {
      await eventsComposable.cancelSignupForEvent(signupEvent.Id)
    }
    else {
      await eventsComposable.signUpForEvent(signupEvent.Id, updatedRoles)
    }
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
    :description="event.Description" :model-value="props.modelValue"
    :title="`${event.Name} - signup`" @update:model-value="emit('update:modelValue', $event)"
  >
    <template #body>
      <DiscordMessageRenderer :content="event.Description" />
    </template>
    <template #image>
      <img v-if="event.PictureUrl" :src="event.PictureUrl" alt="avatar" class="card__image">
    </template>

    <template #actions>
      <BaseButton
        :state="isSignedUpTank ? 'pressed' : 'primary'"
        :title="`Tank (${tankCount})`"
        :tooltip="tankUsers.length > 0 ? tankUsers.join(', ') : 'No signups yet'"
        @clicked="signUp(event, ROLE.Tank)"
      />
      <BaseButton
        :state="isSignedUpHealer ? 'pressed' : 'primary'"
        :title="`Healer (${healerCount})`"
        :tooltip="healerUsers.length > 0 ? healerUsers.join(', ') : 'No signups yet'"
        @clicked="signUp(event, ROLE.Healer)"
      />
      <BaseButton
        :state="isSignedUpMelee ? 'pressed' : 'primary'"
        :title="`Melee (${meleeCount})`"
        :tooltip="meleeUsers.length > 0 ? meleeUsers.join(', ') : 'No signups yet'"
        @clicked="signUp(event, ROLE.Melee)"
      />
      <BaseButton
        :state="isSignedUpCaster ? 'pressed' : 'primary'"
        :title="`Caster (${casterCount})`"
        :tooltip="casterUsers.length > 0 ? casterUsers.join(', ') : 'No signups yet'"
        @clicked="signUp(event, ROLE.Caster)"
      />
      <BaseButton
        :state="isSignedUpRanged ? 'pressed' : 'primary'"
        :title="`Ranged (${rangedCount})`"
        :tooltip="rangedUsers.length > 0 ? rangedUsers.join(', ') : 'No signups yet'"
        @clicked="signUp(event, ROLE.Ranged)"
      />
    </template>
  </BaseModal>
</template>

<style scoped>
.card__image {
  /* zoom in on the image since the fight images have a small white gradient */
  transform: scale(1.1);
}
</style>
