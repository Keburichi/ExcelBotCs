import type { Announcement } from '@/app/announcements.types'
import { ref } from 'vue'
import { AnnouncementsApi } from '@/app/announcements.api'

export function useAnnouncements() {
  const loading = ref(false)
  const error = ref('')
  const announcements = ref<Announcement[]>([])

  async function load() {
    loading.value = true
    error.value = ''
    try {
      announcements.value = await AnnouncementsApi.list()
    }
    catch (e: any) {
      error.value = e.message || 'Failed to load announcements'
    }
    finally {
      loading.value = false
    }
  }

  return { loading, error, announcements, load }
}
