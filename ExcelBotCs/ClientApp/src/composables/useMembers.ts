import type { Member } from '@/features/members/members.types'
import { reactive, ref } from 'vue'
import { MembersApi } from '@/features/members/members.api'

export function useMembers() {
  const loading = ref(false)
  const error = ref('')
  const members = ref<Member[]>([])

  const newMember = reactive<Member>({
    DiscordId: '',
    DiscordName: '',
    DiscordAvatar: '',
    PlayerName: '',
    Subbed: false,
    LodestoneId: '',
    Experience: [],
    Notes: [],
    IsMember: false,
    IsAdmin: false,
    Roles: [],
  })

  const editId = ref<string | null>(null)
  const editBuffer = reactive<Member>({
    DiscordId: '',
    DiscordName: '',
    DiscordAvatar: '',
    PlayerName: '',
    Subbed: false,
    LodestoneId: '',
    Experience: [],
    Notes: [],
    IsMember: false,
    IsAdmin: false,
    Roles: [],
  })

  async function load() {
    loading.value = true
    error.value = ''
    try {
      members.value = await MembersApi.list()
    }
    catch (e: any) {
      error.value = e.message || 'Failed'
    }
    finally {
      loading.value = false
    }
  }

  async function create() {
    try {
      const created = await MembersApi.create(newMember)
      members.value.unshift(created)
      Object.assign(newMember, { Name: '', PlayerName: '', Subbed: false, LodestoneId: '' })
    }
    catch (e: any) {
      error.value = e.message || 'Failed to create member'
    }
  }

  function startEdit(m: Member) {
    editId.value = m.Id ?? null
    Object.assign(editBuffer, m)
  }

  function cancelEdit() {
    editId.value = null
  }

  async function save() {
    if (!editId.value)
      return
    try {
      await MembersApi.update(editId.value, editBuffer)
      const i = members.value.findIndex(x => x.Id === editId.value)
      if (i >= 0)
        members.value[i] = { ...editBuffer, Id: editId.value }
      editId.value = null
    }
    catch (e: any) {
      error.value = e.message || 'Failed to save member'
    }
  }

  return { loading, error, members, newMember, editId, editBuffer, load, create, startEdit, cancelEdit, save }
}
