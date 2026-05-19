import type { Resource } from '@/features/fights/fights.types'
import { ref } from 'vue'
import { ResourcesApi } from '@/features/fights/resources.api'

export function useResources(fightId: string) {
  const loading = ref(false)
  const error = ref('')
  const resources = ref<Resource[]>([])
  const editBuffer = ref<Resource | null>(null)

  async function load() {
    loading.value = true
    error.value = ''
    try {
      resources.value = await ResourcesApi.list(fightId)
    }
    catch (e: any) {
      error.value = e.message || 'Failed to load resources'
    }
    finally {
      loading.value = false
    }
  }

  async function create(resource: Omit<Resource, 'Id' | 'FightId' | 'AuthorId'>) {
    loading.value = true
    error.value = ''
    try {
      const created = await ResourcesApi.create(fightId, resource)
      resources.value.push(created)
      return created
    }
    catch (e: any) {
      error.value = e.message || 'Failed to create resource'
      throw e
    }
    finally {
      loading.value = false
    }
  }

  async function update(id: string, resource: Partial<Resource>) {
    loading.value = true
    error.value = ''
    try {
      await ResourcesApi.update(fightId, id, resource)
      const index = resources.value.findIndex(r => r.Id === id)
      if (index !== -1) {
        resources.value[index] = { ...resources.value[index], ...resource, Id: id }
      }
    }
    catch (e: any) {
      error.value = e.message || 'Failed to update resource'
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
      await ResourcesApi.delete(fightId, id)
      resources.value = resources.value.filter(r => r.Id !== id)
    }
    catch (e: any) {
      error.value = e.message || 'Failed to delete resource'
      throw e
    }
    finally {
      loading.value = false
    }
  }

  function startEdit(resource: Resource) {
    editBuffer.value = { ...resource }
  }

  function cancelEdit() {
    editBuffer.value = null
  }

  return {
    loading,
    error,
    resources,
    editBuffer,
    load,
    create,
    update,
    remove,
    startEdit,
    cancelEdit,
  }
}
