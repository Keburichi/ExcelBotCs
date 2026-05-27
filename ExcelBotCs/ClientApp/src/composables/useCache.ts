import { ref } from 'vue'
import type { CacheEntityStatus, CacheStatusResponse } from '@/features/adminPanel/cache.api'
import { CacheApi } from '@/features/adminPanel/cache.api'

export function useCache() {
  const loading = ref(false)
  const error = ref('')
  const status = ref<CacheStatusResponse | null>(null)
  const selectedEntityType = ref<string | null>(null)
  const entities = ref<any[]>([])
  const entitiesLoading = ref(false)

  async function loadStatus() {
    loading.value = true
    error.value = ''
    try {
      status.value = await CacheApi.getStatus()
    } catch (e: any) {
      error.value = e.message || 'Failed to load cache status'
    } finally {
      loading.value = false
    }
  }

  async function loadEntities(entityType: string) {
    selectedEntityType.value = entityType
    entitiesLoading.value = true
    error.value = ''
    try {
      entities.value = await CacheApi.getEntities(entityType)
    } catch (e: any) {
      error.value = e.message || 'Failed to load entities'
    } finally {
      entitiesLoading.value = false
    }
  }

  async function clearCache(entityType: string) {
    error.value = ''
    try {
      await CacheApi.clear(entityType)
      await loadStatus()
      if (selectedEntityType.value === entityType) {
        entities.value = []
      }
    } catch (e: any) {
      error.value = e.message || 'Failed to clear cache'
    }
  }

  async function fillCache(entityType: string) {
    error.value = ''
    try {
      await CacheApi.fill(entityType)
      await loadStatus()
      if (selectedEntityType.value === entityType) {
        await loadEntities(entityType)
      }
    } catch (e: any) {
      error.value = e.message || 'Failed to fill cache'
    }
  }

  async function clearAll() {
    error.value = ''
    try {
      await CacheApi.clearAll()
      await loadStatus()
      entities.value = []
    } catch (e: any) {
      error.value = e.message || 'Failed to clear all caches'
    }
  }

  async function fillAll() {
    error.value = ''
    try {
      await CacheApi.fillAll()
      await loadStatus()
      if (selectedEntityType.value) {
        await loadEntities(selectedEntityType.value)
      }
    } catch (e: any) {
      error.value = e.message || 'Failed to fill all caches'
    }
  }

  return {
    loading,
    error,
    status,
    selectedEntityType,
    entities,
    entitiesLoading,
    loadStatus,
    loadEntities,
    clearCache,
    fillCache,
    clearAll,
    fillAll,
  }
}
