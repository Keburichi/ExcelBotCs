<script lang="ts" setup>
import type { Member } from '@/features/members/members.types'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import ExperienceTags from '@/components/members/ExperienceTags.vue'
import { useAuth } from '@/composables/useAuth'
import { MembersApi } from '@/features/members/members.api'

const auth = useAuth()

const form = reactive<Partial<Member>>({})

const saving = ref(false)
const verifying = ref(false)
const tokenLoading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const editMode = ref(false)

onMounted(async () => {
  await auth.loadMe()
  hydrateForm()
})

watch(() => auth.user.value, () => hydrateForm())

function hydrateForm() {
  const u = auth.user.value
  if (!u)
    return
  form.Id = u.Id
  form.PlayerName = u.PlayerName
  form.LodestoneId = u.LodestoneId
  form.Subbed = u.Subbed
}

const avatarUrl = computed(() => auth.user.value?.DiscordAvatar || '')
const displayName = computed(() => auth.user.value?.PlayerName || auth.user.value?.DiscordName || 'My Profile')
const discordName = computed(() => auth.user.value?.DiscordName || null)
const localToken = ref('')
const verificationToken = computed(() => localToken.value || auth.user.value?.LodestoneVerificationToken || '')
const hasLodestone = computed(() => !!auth.user.value?.LodestoneId)
const experience = computed(() => auth.user.value?.Experience || [])

function initial(name: string | null | undefined): string {
  if (!name)
    return '?'
  return name.charAt(0).toUpperCase()
}

function startEdit() {
  editMode.value = true
  success.value = null
  error.value = null
}

function cancelEdit() {
  editMode.value = false
  hydrateForm()
}

async function save() {
  if (!form.Id) {
    error.value = 'Cannot update: missing user id.'
    return
  }
  saving.value = true
  error.value = null
  success.value = null
  try {
    const current = auth.user.value!
    const payload: Member = {
      ...current,
      PlayerName: form.PlayerName ?? current.PlayerName,
      LodestoneId: form.LodestoneId ?? current.LodestoneId,
      Subbed: form.Subbed ?? current.Subbed,
    }
    await MembersApi.update(form.Id, payload)
    success.value = 'Profile updated successfully.'
    editMode.value = false
    await auth.loadMe()
  }
  catch (e: any) {
    error.value = e?.message ?? 'Failed to update profile.'
  }
  finally {
    saving.value = false
  }
}

async function generateVerificationToken() {
  if (!form.Id)
    return
  tokenLoading.value = true
  error.value = null
  success.value = null
  try {
    const { token } = await MembersApi.generateLodestoneToken(form.Id)
    localToken.value = token
    await auth.loadMe()
    success.value = 'Generated verification message. Copy it and place it in your Lodestone Bio.'
  }
  catch (e: any) {
    error.value = e?.message ?? 'Failed to generate verification message.'
  }
  finally {
    tokenLoading.value = false
  }
}

async function verifyClaim() {
  if (!form.Id || !form.LodestoneId) {
    error.value = 'Please enter your Lodestone character ID or profile URL.'
    return
  }
  verifying.value = true
  error.value = null
  success.value = null
  try {
    const res = await MembersApi.verifyLodestone(form.Id, String(form.LodestoneId))
    if (res.success) {
      success.value = res.message
      editMode.value = false
      await auth.loadMe()
    }
    else {
      error.value = res.message
    }
  }
  catch (e: any) {
    error.value = e?.message ?? 'Verification failed.'
  }
  finally {
    verifying.value = false
  }
}
</script>

<template>
  <section class="profile">
    <div class="profile__header">
      <img
        v-if="avatarUrl"
        :src="avatarUrl"
        alt="avatar"
        class="profile__avatar"
        referrerpolicy="no-referrer"
      >
      <span v-else class="profile__avatar profile__avatar--placeholder">{{
        initial(displayName)
      }}</span>
      <div class="profile__title">
        <h1 class="profile__name">
          {{ displayName }}
        </h1>
        <p v-if="discordName" class="profile__subtitle">
          @{{ discordName }}
        </p>
        <div class="badges">
          <span v-if="auth.user.value?.IsAdmin" class="badge admin">Admin</span>
          <span v-if="auth.user.value?.IsMember" class="badge member">Member</span>
        </div>
      </div>
      <div class="profile__actions">
        <BaseButton
          v-if="!editMode"
          state="primary"
          title="Edit Profile"
          @clicked="startEdit"
        />
        <BaseButton
          v-else
          state="secondary"
          title="Cancel"
          variant="outlined"
          @clicked="cancelEdit"
        />
      </div>
    </div>

    <div v-if="error" class="alert error">
      {{ error }}
    </div>
    <div v-if="success" class="alert success">
      {{ success }}
    </div>

    <div class="profile__cards">
      <div class="profile__card">
        <h2 class="profile__section-title">
          Profile Details
        </h2>

        <div class="kv-row">
          <span class="kv-label">Discord</span>
          <span class="kv-value muted">
            {{ auth.user.value?.DiscordName }}
          </span>
        </div>

        <div class="kv-row">
          <label for="playerName" class="kv-label">Player Name</label>
          <template v-if="editMode">
            <input id="playerName" v-model="form.PlayerName" placeholder="Your in-game name">
          </template>
          <span v-else class="kv-value">
            {{ auth.user.value?.PlayerName }}
          </span>
        </div>

        <div class="kv-row">
          <label for="lodestoneId" class="kv-label">Lodestone ID</label>
          <template v-if="editMode">
            <input id="lodestoneId" v-model="form.LodestoneId" placeholder="Character ID or Lodestone URL">
          </template>
          <span v-else class="kv-value">
            {{ auth.user.value?.LodestoneId || 'Not linked' }}
          </span>
        </div>

        <div v-if="editMode && !auth.user.value?.LodestoneId" class="kv-row kv-row--verification">
          <span class="kv-label">Verification</span>
          <div class="verification-flow">
            <ol class="verification-steps">
              <li>Enter your Character Id or Lodestone Url</li>
              <li>Click "Generate message"</li>
              <li>Paste the message into your Lodestone Bio</li>
              <li>Click "Verify now"</li>
            </ol>
            <div class="verification-token">
              <span class="verification-token__label">Message for your Lodestone Bio:</span>
              <code class="verification-token__code">{{ verificationToken || 'ExcelsiorFc-XXXXXXXX...' }}</code>
            </div>
            <div class="form-actions">
              <BaseButton
                :disabled="tokenLoading"
                :title="tokenLoading ? 'Generating...' : 'Generate message'"
                state="secondary"
                variant="outlined"
                @clicked="generateVerificationToken"
              />
              <BaseButton
                :disabled="verifying || !form.LodestoneId"
                :title="verifying ? 'Verifying...' : 'Verify now'"
                state="primary"
                @clicked="verifyClaim"
              />
            </div>
          </div>
        </div>

        <div class="kv-row">
          <span class="kv-label">Subbed</span>
          <template v-if="editMode">
            <label class="switch">
              <input v-model="form.Subbed" type="checkbox">
              <span class="slider" />
            </label>
          </template>
          <span v-else class="kv-value">
            <span
              :class="[auth.user.value?.Subbed ? 'on' : 'off']"
              class="pill"
            >{{ auth.user.value?.Subbed ? 'Yes' : 'No' }}</span>
          </span>
        </div>

        <div v-if="editMode" class="form-actions">
          <BaseButton
            :disabled="saving"
            :title="saving ? 'Saving...' : 'Save Changes'"
            state="primary"
            @clicked="save"
          />
        </div>
      </div>

      <div v-if="auth.user.value?.IsAdmin" class="profile__card">
        <h2 class="profile__section-title">
          Experience
        </h2>
        <template v-if="hasLodestone">
          <ExperienceTags :experience="experience" />
          <p v-if="!experience.length" class="muted">
            No experience recorded yet.
          </p>
        </template>
        <template v-else>
          <p class="muted">
            Connect your Lodestone to see your in-game experience here.
          </p>
        </template>
      </div>

      <div class="profile__card">
        <h2 class="profile__section-title">
          Roles
        </h2>
        <div class="chips">
          <span v-for="r in auth.user.value?.Roles" :key="r.Id || r.Name" class="chip">{{ r.Name }}</span>
          <span v-if="!auth.user.value?.Roles?.length" class="muted">No roles assigned</span>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.profile {
  max-width: 900px;
  margin: 0 auto;
}

/* Header */
.profile__header {
  display: flex;
  align-items: center;
  gap: 1.25rem;
  margin-bottom: 1.5rem;
}

.profile__avatar {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1),
  inset 0 0 0 2px rgba(255, 255, 255, 0.3);
}

.profile__avatar--placeholder {
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  color: #fff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 1.75rem;
  line-height: 1;
}

.profile__title {
  flex: 1;
  min-width: 0;
}

.profile__name {
  font-size: 1.5rem;
  font-weight: 600;
  line-height: 1.3;
  margin: 0 0 0.125rem;
}

.profile__subtitle {
  margin: 0 0 0.5rem;
  color: var(--muted);
  font-size: 0.9rem;
}

.profile__actions {
  flex-shrink: 0;
  align-self: flex-start;
  margin-top: 0.25rem;
}

/* Cards stack */
.profile__cards {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.profile__card {
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  padding: 1.25rem 1.5rem;
}

:root[data-theme='dark'] .profile__card {
  background: rgba(18, 26, 45, 0.7);
  border-color: rgba(255, 255, 255, 0.15);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .profile__card {
    background: rgba(18, 26, 45, 0.7);
    border-color: rgba(255, 255, 255, 0.15);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.profile__section-title {
  font-size: 1.125rem;
  font-weight: 600;
  line-height: 1.4;
  margin: 0 0 1rem;
}

/* Key-value rows */
.kv-row {
  display: grid;
  grid-template-columns: 140px 1fr;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem 0;
  border-bottom: 1px solid var(--border);
}

.kv-row:last-child {
  border-bottom: none;
}

.kv-row--verification {
  align-items: start;
}

.kv-label {
  color: var(--muted);
  font-size: 0.875rem;
  font-weight: 500;
}

.kv-value {
  font-weight: 500;
}

/* Verification flow */
.verification-flow {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.verification-steps {
  margin: 0;
  padding-left: 1.25rem;
  list-style: decimal;
  color: var(--muted);
  font-size: 0.875rem;
  line-height: 1.6;
}

.verification-token {
  padding: 0.75rem 1rem;
  border-radius: 8px;
  background: var(--muted-bg);
  border: 1px solid var(--border);
}

:root[data-theme='dark'] .verification-token {
  background: rgba(15, 23, 42, 0.5);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .verification-token {
    background: rgba(15, 23, 42, 0.5);
  }
}

.verification-token__label {
  display: block;
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--muted);
  margin-bottom: 0.375rem;
}

.verification-token__code {
  display: block;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 0.8125rem;
  color: var(--fg);
  word-break: break-all;
  user-select: all;
}

/* Responsive */
@media (max-width: 640px) {
  .profile__header {
    flex-wrap: wrap;
    gap: 1rem;
  }

  .profile__avatar {
    width: 64px;
    height: 64px;
  }

  .profile__avatar--placeholder {
    font-size: 1.5rem;
  }

  .profile__name {
    font-size: 1.25rem;
  }

  .profile__actions {
    width: 100%;
  }

  .profile__actions :deep(.base-btn) {
    width: 100%;
  }

  .profile__card {
    padding: 1rem;
  }

  .kv-row {
    grid-template-columns: 1fr;
    gap: 0.25rem;
  }
}
</style>
