<script setup lang="ts">
import {computed, onMounted, reactive, ref, watch} from 'vue'
import type {Member} from '@/features/members/members.types'
import {MembersApi} from '@/features/members/members.api'
import {useAuth} from "@/composables/useAuth";

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
  if (!u) return
  form.Id = u.Id
  form.PlayerName = u.PlayerName
  form.LodestoneId = u.LodestoneId
  form.Subbed = u.Subbed
}

const avatarUrl = computed(() => auth.user.value?.DiscordAvatar || '')
const displayName = computed(() => auth.user.value?.PlayerName || auth.user.value?.DiscordName || 'My Profile')
const verificationToken = computed(() => auth.user.value?.LodestoneVerificationToken || '')

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
  } catch (e: any) {
    error.value = e?.message ?? 'Failed to update profile.'
  } finally {
    saving.value = false
  }
}

async function generateVerificationToken() {
  if (!form.Id) return
  tokenLoading.value = true
  error.value = null
  success.value = null
  try {
    await MembersApi.generateLodestoneToken(form.Id)
    await auth.loadMe()
    success.value = 'Generated verification message. Copy it and place it in your Lodestone Bio.'
  } catch (e: any) {
    error.value = e?.message ?? 'Failed to generate verification message.'
  } finally {
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
    } else {
      error.value = res.message
    }
  } catch (e: any) {
    error.value = e?.message ?? 'Verification failed.'
  } finally {
    verifying.value = false
  }
}
</script>

<template>
  <section class="profile container">
    <div class="profile__header">
      <img v-if="avatarUrl" :src="avatarUrl" alt="avatar" class="profile__avatar" />
      <div class="profile__title">
        <h1>{{ displayName }}</h1>
        <p class="profile__subtitle">@{{ auth.user.value?.DiscordName }}</p>
        <div class="badges">
          <span v-if="auth.user.value?.IsAdmin" class="badge admin">Admin</span>
          <span v-if="auth.user.value?.IsMember" class="badge member">Member</span>
        </div>
      </div>
      <div class="profile__actions">
        <button v-if="!editMode" class="btn" @click="startEdit">Edit Profile</button>
        <button v-else class="btn ghost" @click="cancelEdit">Cancel</button>
      </div>
    </div>

    <div v-if="error" class="alert error">{{ error }}</div>
    <div v-if="success" class="alert success">{{ success }}</div>

    <div class="cards_container--large">
      <div class="card">
        <h2>Profile Details</h2>

        <div class="kv-row">
          <label>Discord</label>
          <div class="kv-value muted">{{ auth.user.value?.DiscordName }}</div>
        </div>

        <div class="kv-row" :class="{ editable: editMode }">
          <label for="playerName">Player Name</label>
          <template v-if="editMode">
            <input id="playerName" v-model="form.PlayerName" placeholder="Your in-game name" />
          </template>
          <div v-else class="kv-value">{{ auth.user.value?.PlayerName }}</div>
        </div>

        <div class="kv-row" :class="{ editable: editMode }">
          <label for="lodestoneId">Lodestone ID</label>
          <template v-if="editMode">
            <input id="lodestoneId" v-model="form.LodestoneId" placeholder="Character ID or Lodestone URL" />
          </template>
          <div v-else class="kv-value">{{ auth.user.value?.LodestoneId || '—' }}</div>
        </div>

        <div class="kv-row" v-if="editMode && !auth.user.value?.LodestoneId">
          <label>Verification</label>
          <div>
            <p class="muted">To prove ownership: 1) Click "Generate message" 2) Paste it into your Lodestone Bio 3) Click "Verify now".</p>
            <div class="alert">
              <div><strong>Message to place in your Lodestone Bio:</strong></div>
              <div class="kv-value"><code>{{ verificationToken || 'ExcelsiorFc-XXXXXXXX...' }}</code></div>
            </div>
            <div class="form-actions">
              <button class="btn secondary" :disabled="tokenLoading" @click="generateVerificationToken">
                <span v-if="tokenLoading">Generating...</span>
                <span v-else>Generate message</span>
              </button>
              <button class="btn primary" :disabled="verifying || !form.LodestoneId" @click="verifyClaim">
                <span v-if="verifying">Verifying...</span>
                <span v-else>Verify now</span>
              </button>
            </div>
          </div>
        </div>

        <div class="kv-row" :class="{ editable: editMode }">
          <label>Subbed</label>
          <template v-if="editMode">
            <label class="switch">
              <input type="checkbox" v-model="form.Subbed">
              <span class="slider" />
            </label>
          </template>
          <div v-else class="kv-value">
            <span :class="['pill', auth.user.value?.Subbed ? 'on' : 'off']">{{ auth.user.value?.Subbed ? 'Yes' : 'No' }}</span>
          </div>
        </div>

        <div v-if="editMode" class="form-actions">
          <button class="btn primary" :disabled="saving" @click="save">
            <span v-if="saving">Saving...</span>
            <span v-else>Save Changes</span>
          </button>
        </div>
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