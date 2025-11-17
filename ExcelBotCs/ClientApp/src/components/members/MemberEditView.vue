<script setup lang="ts">
import type { Member, MemberNote } from '@/features/members/members.types'
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import ExperienceTags from '@/components/members/ExperienceTags.vue'
import { useAuth } from '@/composables/useAuth'
import { MembersApi } from '@/features/members/members.api'

const router = useRouter()
const route = useRoute()
const { user, isAdmin, loadMe } = useAuth()

const loading = ref(false)
const error = ref('')

const form = reactive<Member>({
  Id: '',
  DiscordName: '',
  DiscordAvatar: '',
  LodestoneId: '',
  LodestoneVerificationToken: '',
  PlayerName: '',
  Subbed: false,
  DiscordId: '',
  IsAdmin: false,
  IsMember: false,
  Experience: [],
  Notes: [],
  Roles: [],
})

const addNoteOpen = ref(false)
const addNoteBuffer = ref<MemberNote>()
const editNoteOpen = ref(false)
const editNoteBuffer = ref<MemberNote>()

function editNote(note: MemberNote) {
  editNoteBuffer.value = note
}

onMounted(async () => {
  loading.value = true
  try {
    const memberData = await MembersApi.get(route.params.id as string)
    if (memberData) {
      Object.assign(form, memberData)
    }
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to load event'
  }
  finally {
    loading.value = false
  }
})
</script>

<template>
  <section>
    <h1 class="text-3xl font-bold">
      Edit {{ form.DiscordName }}
    </h1>
    <p v-if="error" class="error">
      {{ error }}
    </p>
    <form class="form">
      <div class="form-row">
        <label>Character Id</label>
        <input v-model="form.LodestoneId" type="text" placeholder="Character Lodestone Id">
      </div>

      <div class="form-row">
        <label>Lodestone Verification Toke</label>
        <input v-model="form.LodestoneVerificationToken" type="text" placeholder="Lodestone verification token" disabled>
      </div>

      <div class="form-row-checkbox">
        <input :id="form.DiscordId" v-model="form.Subbed" :name="form.DiscordId" type="checkbox" placeholder="Is player subbed?">
        <label :for="form.DiscordId">Subbed?</label>
      </div>

      <div class="form-row">
        <label>Experience:</label>
        <div v-if="form.Experience?.length" class="experience-container">
          <ExperienceTags :experience="form.Experience" />
        </div>
        <p v-else class="no-experience">
          No cleared content yet
        </p>
      </div>

      <div class="form-row">
        <h1>Notes</h1>
        <table>
          <thead>
            <tr>
              <th>Author</th>
              <th>Note</th>
              <th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in form.Notes" :key="m.Id">
              <td>{{ m.Author?.DiscordName }}</td>
              <td>{{ m.Note }}</td>
              <td><BaseButton title="Edit" size="small" @clicked="editNote(m)" /></td>
            </tr>
          </tbody>
        </table>
      </div>
    </form>

    <BaseButton title="Add Note" size="small" @clicked="addNoteOpen = true" />
  </section>
</template>

<style scoped>
.error {
  color: #c62828;
}

input{
  max-width: 720px;
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 12px 0;
}

.form-row-checkbox {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
  margin: 12px 0;
}

.form-row-checkbox input[type="checkbox"] {
  width: 1.25rem;
  height: 1.25rem;
  cursor: pointer;
}

.form-row-checkbox label {
  cursor: pointer;
  margin: 0;
  font-weight: 500;
}

.experience-container {
  padding: 0.75rem;
  background: var(--card, #fff);
  border: 1px solid var(--border, #e5e7eb);
  border-radius: 0.5rem;
}

.no-experience {
  color: var(--muted, #6b7280);
  font-style: italic;
  margin: 0;
}
</style>
