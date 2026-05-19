<script setup lang="ts">
import type { FCEvent, GuildEmoji, Role } from '@/features/events/events.types'
import { computed, onMounted, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import DiscordMessageRenderer from '@/components/DiscordMessageRenderer.vue'
import { useAuth } from '@/composables/useAuth'
import { useEvents } from '@/composables/useEvents'
import { useMembers } from '@/composables/useMembers'
import { ROLE } from '@/features/events/events.types'
import { EventsApi } from '@/features/events/events.api'

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
const signingUp = ref<string | null>(null)
const errorMessage = ref('')

watch(() => props.event, (newEvent) => {
  fcEvent.value = newEvent
}, { deep: true })

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

const totalSignups = computed(() => {
  if (!fcEvent.value?.Signups) return 0
  return fcEvent.value.Signups.length
})

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
  signingUp.value = slug
  errorMessage.value = ''

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
  catch {
    errorMessage.value = 'Failed to update signup. Please try again.'
  }
  finally {
    signingUp.value = null
  }
}

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

const roles = [
  { key: ROLE.Tank, label: 'Tank' },
  { key: ROLE.Healer, label: 'Healer' },
  { key: ROLE.Melee, label: 'Melee' },
  { key: ROLE.Caster, label: 'Caster' },
  { key: ROLE.Ranged, label: 'Ranged' },
] as const

async function signUp(signupEvent: FCEvent, role: Role) {
  signingUp.value = String(role)
  errorMessage.value = ''

  const currentSignup = fcEvent.value?.Signups?.find(s => s.DiscordUserId === user.value?.DiscordId)
  const currentRoles = currentSignup?.Roles ?? []

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
  catch {
    errorMessage.value = 'Failed to update signup. Please try again.'
  }
  finally {
    signingUp.value = null
  }
}
</script>

<template>
  <BaseModal
    :model-value="props.modelValue"
    :title="event.Name"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <template #image>
      <img v-if="event.PictureUrl" :src="event.PictureUrl" alt="" class="card__image">
    </template>

    <template #body>
      <DiscordMessageRenderer v-if="event.Description" :content="event.Description" />

      <div class="signup-section">
        <div class="signup-header">
          <span class="signup-label">Sign up</span>
          <span class="signup-count">{{ totalSignups }} signed up</span>
        </div>

        <div class="signup-buttons">
          <template v-if="usesCustomButtons">
            <button
              v-for="config in event.SignupButtonConfigs"
              :key="config.Slug"
              class="signup-btn"
              :class="{ 'signup-btn--active': isSignedUpForSlug(config.Slug), 'signup-btn--loading': signingUp === config.Slug }"
              :disabled="signingUp !== null"
              :data-tooltip="getSignedUpUsersForSlug(config.Slug).length > 0 ? getSignedUpUsersForSlug(config.Slug).join(', ') : undefined"
              type="button"
              @click="signUpBySlug(event, config.Slug)"
            >
              <img v-if="getEmojiUrl(config.EmojiId)" :src="getEmojiUrl(config.EmojiId)!" alt="" class="signup-btn__emoji">
              <span class="signup-btn__label">{{ config.Label }}</span>
              <span class="signup-btn__count">{{ getSignupCountForSlug(config.Slug) }}</span>
            </button>
          </template>

          <template v-else>
            <button
              v-for="r in roles"
              :key="r.key"
              class="signup-btn"
              :class="{ 'signup-btn--active': isSignedUpForRole(r.key), 'signup-btn--loading': signingUp === String(r.key) }"
              :disabled="signingUp !== null"
              :data-tooltip="getSignedUpUsersForRole(r.key).length > 0 ? getSignedUpUsersForRole(r.key).join(', ') : undefined"
              type="button"
              @click="signUp(event, r.key)"
            >
              <span class="signup-btn__label">{{ r.label }}</span>
              <span class="signup-btn__count">{{ getSignupCountForRole(r.key) }}</span>
            </button>
          </template>
        </div>

        <p v-if="errorMessage" class="signup-error">{{ errorMessage }}</p>
      </div>
    </template>

    <template #actions>
      <BaseButton
        state="secondary"
        title="Close"
        @clicked="emit('update:modelValue', false)"
      />
    </template>
  </BaseModal>
</template>

<style scoped>
.card__image {
  transform: scale(1.1);
}

.signup-section {
  margin-top: 1.25rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border);
}

.signup-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  margin-bottom: 0.75rem;
}

.signup-label {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--fg);
}

.signup-count {
  font-size: 0.8rem;
  color: var(--muted);
}

.signup-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.signup-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  border: 1px solid var(--border);
  background: var(--muted-bg);
  color: var(--fg);
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: border-color 0.2s, background 0.2s, opacity 0.2s;
}

.signup-btn:hover:not(:disabled) {
  border-color: var(--link);
  background: color-mix(in oklab, var(--card) 90%, var(--link) 10%);
}

.signup-btn:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

.signup-btn:active:not(:disabled) {
  transform: scale(0.97);
}

.signup-btn:disabled {
  cursor: not-allowed;
}

.signup-btn--active {
  border-color: #059669;
  background: rgba(5, 150, 105, 0.1);
  color: #059669;
}

:root[data-theme='dark'] .signup-btn--active {
  border-color: #34d399;
  background: rgba(52, 211, 153, 0.1);
  color: #34d399;
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .signup-btn--active {
    border-color: #34d399;
    background: rgba(52, 211, 153, 0.1);
    color: #34d399;
  }
}

.signup-btn--active:hover:not(:disabled) {
  border-color: #047857;
  background: rgba(5, 150, 105, 0.15);
}

:root[data-theme='dark'] .signup-btn--active:hover:not(:disabled) {
  border-color: #6ee7b7;
  background: rgba(52, 211, 153, 0.15);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .signup-btn--active:hover:not(:disabled) {
    border-color: #6ee7b7;
    background: rgba(52, 211, 153, 0.15);
  }
}

.signup-btn--loading {
  opacity: 0.6;
}

.signup-btn__emoji {
  width: 1.125rem;
  height: 1.125rem;
  object-fit: contain;
}

.signup-btn__label {
  white-space: nowrap;
}

.signup-btn__count {
  font-size: 0.75rem;
  font-weight: 700;
  min-width: 1.25rem;
  height: 1.25rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  background: var(--border);
  color: var(--muted);
}

.signup-btn--active .signup-btn__count {
  background: rgba(5, 150, 105, 0.2);
  color: inherit;
}

.signup-error {
  margin: 0.75rem 0 0;
  padding: 0.5rem 0.75rem;
  background: var(--alert-error-bg, rgba(220, 38, 38, 0.1));
  border: 1px solid var(--alert-error-border, rgba(220, 38, 38, 0.3));
  border-radius: 8px;
  color: var(--alert-error-fg, #dc2626);
  font-size: 0.85rem;
  font-weight: 500;
}
</style>
