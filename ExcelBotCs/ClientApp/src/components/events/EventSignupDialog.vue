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
import { EventsApi } from '@/features/events/events.api'
import type { GuildEmoji } from '@/features/events/events.types'

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
const guildEmojis = ref<GuildEmoji[]>([])

// Watch for event prop changes
watch(() => props.event, (newEvent) => {
  fcEvent.value = newEvent
}, { deep: true })

// Load members data and emojis when component mounts
onMounted(async () => {
  if (members.members.value.length === 0) {
    members.load()
  }
  try {
    guildEmojis.value = await EventsApi.getGuildEmojis()
  }
  catch {
    // Emojis are cosmetic, fail silently
  }
})

const usesCustomButtons = computed(() =>
  fcEvent.value?.SignupButtonConfigs && fcEvent.value.SignupButtonConfigs.length > 0,
)

// --- Custom button helpers ---

function getEmojiUrl(emojiId?: string): string | null {
  if (!emojiId) return null
  const emoji = guildEmojis.value.find(e => e.Id === emojiId)
  if (emoji) return emoji.Url
  return `https://cdn.discordapp.com/emojis/${emojiId}.webp?size=20`
}

function isSignedUpForSlug(slug: string): boolean {
  if (!user.value?.DiscordId || !fcEvent.value?.Signups)
    return false

  return fcEvent.value.Signups.some(signup =>
    signup.DiscordUserId === user.value?.DiscordId && signup.SignupSlugs?.includes(slug),
  )
}

function getSignupCountForSlug(slug: string): number {
  if (!fcEvent.value?.Signups)
    return 0

  return fcEvent.value.Signups.filter(signup =>
    signup.SignupSlugs?.includes(slug),
  ).length
}

function getSignedUpUsersForSlug(slug: string): string[] {
  if (!fcEvent.value?.Signups)
    return []

  return fcEvent.value.Signups
    .filter(signup => signup.SignupSlugs?.includes(slug))
    .map((signup) => {
      const member = members.members.value.find(m => m.DiscordId === signup.DiscordUserId)
      if (!member)
        return signup.DiscordUserId
      return member.PlayerName || member.DiscordName
    })
}

async function signUpBySlug(signupEvent: FCEvent, slug: string) {
  const currentSignup = fcEvent.value?.Signups?.find(s => s.DiscordUserId === user.value?.DiscordId)
  const currentSlugs = currentSignup?.SignupSlugs ?? []

  let updatedSlugs: string[]
  if (currentSlugs.includes(slug)) {
    updatedSlugs = currentSlugs.filter(s => s !== slug)
  }
  else {
    updatedSlugs = [...currentSlugs, slug]
  }

  try {
    if (updatedSlugs.length === 0 && currentSignup) {
      await eventsComposable.cancelSignupForEvent(signupEvent.Id)
    }
    else {
      await eventsComposable.signUpWithSlugs(signupEvent.Id, updatedSlugs)
    }
    fcEvent.value = await eventsComposable.getEvent(signupEvent.Id)
  }
  catch (error) {
    console.error('Error signing up:', error)
    alert('Error signing up. Please try again.')
  }
}

// --- Legacy role helpers ---

function isSignedUpForRole(role: Role): boolean {
  if (!user.value?.DiscordId || !fcEvent.value?.Signups)
    return false

  return fcEvent.value.Signups.some(signup =>
    signup.DiscordUserId === user.value?.DiscordId && signup.Roles.includes(role),
  )
}

function getSignupCountForRole(role: Role): number {
  if (!fcEvent.value?.Signups)
    return 0

  return fcEvent.value.Signups.filter(signup =>
    signup.Roles.includes(role),
  ).length
}

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

// Computed properties for each role (legacy)
const isSignedUpTank = computed(() => isSignedUpForRole(ROLE.Tank))
const isSignedUpHealer = computed(() => isSignedUpForRole(ROLE.Healer))
const isSignedUpMelee = computed(() => isSignedUpForRole(ROLE.Melee))
const isSignedUpCaster = computed(() => isSignedUpForRole(ROLE.Caster))
const isSignedUpRanged = computed(() => isSignedUpForRole(ROLE.Ranged))

// Signup counts for each role (legacy)
const tankCount = computed(() => getSignupCountForRole(ROLE.Tank))
const healerCount = computed(() => getSignupCountForRole(ROLE.Healer))
const meleeCount = computed(() => getSignupCountForRole(ROLE.Melee))
const casterCount = computed(() => getSignupCountForRole(ROLE.Caster))
const rangedCount = computed(() => getSignupCountForRole(ROLE.Ranged))

// Signed up users for tooltips (legacy)
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
      <!-- Custom buttons mode -->
      <template v-if="usesCustomButtons">
        <BaseButton
          v-for="config in event.SignupButtonConfigs"
          :key="config.Slug"
          :state="isSignedUpForSlug(config.Slug) ? 'pressed' : 'primary'"
          :title="`${config.Label} (${getSignupCountForSlug(config.Slug)})`"
          :tooltip="getSignedUpUsersForSlug(config.Slug).length > 0 ? getSignedUpUsersForSlug(config.Slug).join(', ') : 'No signups yet'"
          @clicked="signUpBySlug(event, config.Slug)"
        >
          <template v-if="getEmojiUrl(config.EmojiId)" #icon>
            <img :src="getEmojiUrl(config.EmojiId)!" alt="" class="button-emoji">
          </template>
        </BaseButton>
      </template>

      <!-- Legacy role buttons -->
      <template v-else>
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
    </template>
  </BaseModal>
</template>

<style scoped>
.card__image {
  /* zoom in on the image since the fight images have a small white gradient */
  transform: scale(1.1);
}

.button-emoji {
  width: 20px;
  height: 20px;
  vertical-align: middle;
}
</style>
