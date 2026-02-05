<script lang="ts" setup>
import type { Member } from '@/features/members/members.types'
import { computed, onMounted, reactive, ref, watch } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import ExperienceTags from '@/components/members/ExperienceTags.vue'
import { useAuth } from '@/composables/useAuth'
import { MembersApi } from '@/features/members/members.api'

const auth = useAuth()

// Local editable copy of the user
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
const verificationToken = computed(() => auth.user.value?.LodestoneVerificationToken || '')
const hasLodestone = computed(() => !!auth.user.value?.LodestoneId)
const experience = computed(() => auth.user.value?.Experience || [])

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
    // Build updated payload: keep non-editable fields from current user
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
    await auth.loadMe() // refresh local user
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
    await MembersApi.generateLodestoneToken(form.Id)
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
  <section class="profile container">
    <div class="profile__header">
      <img v-if="avatarUrl" :src="avatarUrl" alt="avatar" class="profile__avatar">
      <div class="profile__title">
        <h1>{{ displayName }}</h1>
        <p class="profile__subtitle">
          @{{ auth.user.value?.DiscordName }}
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

    <div class="cards_container--large">
      <div class="card">
        <h2>Profile Details</h2>

        <div class="kv-row">
          <label>Discord</label>
          <div class="kv-value muted">
            {{ auth.user.value?.DiscordName }}
          </div>
        </div>

        <div :class="{ editable: editMode }" class="kv-row">
          <label for="playerName">Player Name</label>
          <template v-if="editMode">
            <input id="playerName" v-model="form.PlayerName" placeholder="Your in-game name">
          </template>
          <div v-else class="kv-value">
            {{ auth.user.value?.PlayerName }}
          </div>
        </div>

        <div :class="{ editable: editMode }" class="kv-row">
          <label for="lodestoneId">Lodestone ID</label>
          <template v-if="editMode">
            <input id="lodestoneId" v-model="form.LodestoneId" placeholder="Character ID or Lodestone URL">
          </template>
          <div v-else class="kv-value">
            {{ auth.user.value?.LodestoneId || '—' }}
          </div>
        </div>

        <div v-if="editMode && !auth.user.value?.LodestoneId" class="kv-row">
          <label>Verification</label>
          <div>
            <p class="muted">
              To prove ownership: 1) Click "Generate message" 2) Paste it into your Lodestone Bio 3) Click "Verify now".
            </p>
            <div class="alert">
              <div><strong>Message to place in your Lodestone Bio:</strong></div>
              <div class="kv-value">
                <code>{{ verificationToken || 'ExcelsiorFc-XXXXXXXX...' }}</code>
              </div>
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

        <div :class="{ editable: editMode }" class="kv-row">
          <label>Subbed</label>
          <template v-if="editMode">
            <label class="switch">
              <input v-model="form.Subbed" type="checkbox">
              <span class="slider" />
            </label>
          </template>
          <div v-else class="kv-value">
            <span
              :class="[auth.user.value?.Subbed ? 'on' : 'off']"
              class="pill"
            >{{ auth.user.value?.Subbed ? 'Yes' : 'No' }}</span>
          </div>
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

      <div v-if="auth.user.value?.IsAdmin" class="card">
        <h2>Experience</h2>
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

      <div class="card">
        <h2>Roles</h2>
        <div class="chips">
          <span v-for="r in auth.user.value?.Roles" :key="r.Id || r.Name" class="chip">{{ r.Name }}</span>
          <span v-if="!auth.user.value?.Roles?.length" class="muted">No roles assigned</span>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
/* Profile layout helpers */
.profile {
  max-width: 900px;
  margin: 0 auto;
  padding: 28px 24px;
}

.profile__header {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-bottom: 24px;
}

.profile__avatar {
  width: 96px;
  height: 96px;
  border-radius: 50%;
  object-fit: cover;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.profile__title {
  flex: 1;
}

.profile__title h1 {
  margin: 0 0 2px;
  font-size: 28px;
}

.profile__subtitle {
  margin: 2px 0 10px;
  color: var(--muted);
}

.profile__actions {
  display: flex;
  gap: 8px;
}

/* Improve spacing inside profile cards that don't use card__body wrappers */
.profile .card {
  padding: 16px;
}

.profile .card h2 {
  margin: 0 0 12px;
}

/* Make each key/value row more breathable */
.profile .kv-row {
  padding: 12px 0;
  border-bottom: 1px dashed var(--border);
}

.profile .kv-row + .kv-row {
  margin-top: 6px;
}

.profile .kv-row:last-child {
  border-bottom: none;
}

/* Experience list on profile */
.experience-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: grid;
  gap: 8px;
}

.experience-item {
  padding: 10px 12px;
  border: 1px dashed var(--border);
  border-radius: 10px;
  background: color-mix(in oklab, var(--card) 96%, transparent 4%);
}

.experience-item__main {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.experience-item__name {
  font-weight: 600;
}

.experience-item__type {
  color: var(--muted);
  font-size: 12px;
}

.experience-item__meta {
  margin-top: 6px;
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

/* Key-value row specific to detail views */
.kv-row {
  display: grid;
  grid-template-columns: 160px 1fr;
  align-items: center;
  gap: 12px;
  padding: 10px 0;
  border-bottom: 1px dashed var(--border);
}

.kv-row:last-child {
  border-bottom: none;
}

.kv-row label {
  color: var(--muted);
  font-size: 14px;
}

.kv-value {
  font-weight: 500;
}

.kv-value.muted, .muted {
  color: #9ca3af;
  font-weight: 400;
}

.kv-row input {
  width: 100%;
  padding: 8px 10px;
  border: 1px solid var(--input-border);
  border-radius: 8px;
  font-size: 14px;
  background: var(--input-bg);
  color: var(--input-fg);
}
</style>
