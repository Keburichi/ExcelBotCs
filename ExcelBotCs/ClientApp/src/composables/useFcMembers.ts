import type { FcMember } from '@/features/fcMembers/fcMembers.types'
import { reactive, ref } from 'vue'
import { FcMembersApi } from '@/features/fcMembers/fcMembers.api'

export function useFcMembers() {
  const loading = ref(false)
  const error = ref('')
  const members = ref<FcMember[]>([])

  const newMember = reactive<FcMember>({
    Title: '',
    Name: '',
    CharacterId: '',
    FcRank: '',
    Avatar: '',
    Bio: '',
  })

  const editId = ref<string | null>(null)
  const editBuffer = reactive<FcMember>({
    Title: '',
    Name: '',
    CharacterId: '',
    FcRank: '',
    Avatar: '',
    Bio: '',
  })

  async function load() {
    loading.value = true
    error.value = ''
    try {
      members.value = await FcMembersApi.list()
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
      const created = await FcMembersApi.create(newMember)
      members.value.unshift(created)
      Object.assign(newMember, { Name: '', PlayerName: '', Subbed: false, LodestoneId: '' })
    }
    catch (e: any) {
      error.value = e.message || 'Failed to create member'
    }
  }

  function startEdit(m: FcMember) {
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
      await FcMembersApi.update(editId.value, editBuffer)
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
