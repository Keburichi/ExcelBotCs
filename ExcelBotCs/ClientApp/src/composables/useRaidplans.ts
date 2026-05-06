import type { Raidplan } from '@/features/fights/fights.types'
import { reactive, ref } from 'vue'
import { RaidplansApi } from '@/features/fights/raidplans.api'

export function useRaidplans(fightId: string) {
  const loading = ref(false)
  const error = ref('')
  const raidplans = ref<Raidplan[]>([])

  const newRaidplan = reactive<Raidplan>({
    Name: '',
    Description: '',
    Url: '',
    AuthorId: '',
  })

  const editBuffer = ref<Raidplan | null>(null)

  async function load() {
    loading.value = true
    error.value = ''
    try {
      raidplans.value = await RaidplansApi.list(fightId)
    }
    catch (e: any) {
      error.value = e.message || 'Failed to load raidplans'
    }
    finally {
      loading.value = false
    }
  }

  async function create(raidplan: Raidplan) {
    loading.value = true
    error.value = ''
    try {
      const created = await RaidplansApi.create(fightId, raidplan)
      raidplans.value.push(created)
      return created
    }
    catch (e: any) {
      error.value = e.message || 'Failed to create raidplan'
      throw e
    }
    finally {
      loading.value = false
    }
  }

  async function update(id: string, raidplan: Raidplan) {
    loading.value = true
    error.value = ''
    try {
      await RaidplansApi.update(fightId, id, raidplan)
      const index = raidplans.value.findIndex(r => r.Id === id)
      if (index !== -1) {
        raidplans.value[index] = { ...raidplan, Id: id }
      }
    }
    catch (e: any) {
      error.value = e.message || 'Failed to update raidplan'
      throw e
    }
    finally {
      loading.value = false
    }
  }

  async function remove(id: string) {
    loading.value = true
    error.value = ''
    try {
      await RaidplansApi.delete(fightId, id)
      raidplans.value = raidplans.value.filter(r => r.Id !== id)
    }
    catch (e: any) {
      error.value = e.message || 'Failed to delete raidplan'
      throw e
    }
    finally {
      loading.value = false
    }
  }

  function startEdit(raidplan: Raidplan) {
    editBuffer.value = { ...raidplan }
  }

  function cancelEdit() {
    editBuffer.value = null
  }

  return {
    loading,
    error,
    raidplans,
    newRaidplan,
    editBuffer,
    load,
    create,
    update,
    remove,
    startEdit,
    cancelEdit,
  }
}
