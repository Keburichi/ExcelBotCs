import type { Fight } from '@/features/fights/fights.types'
import { reactive, ref } from 'vue'
import { FightsApi } from '@/features/fights/fights.api'
import { FightType } from '@/features/fights/fights.types'

export function useFights() {
  const loading = ref(false)
  const error = ref('')
  const fights = ref<Fight[]>([])

  const newFight = reactive<Fight>({
    Name: '',
    Description: '',
    Raidplans: [],
    ImageUrl: '',
    Type: FightType.Extreme,
  })

  const editId = ref<string | null>(null)

  async function getFights() {
    loading.value = true
    error.value = ''
    try {
      fights.value = await FightsApi.list()
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
      const created = await FightsApi.create(newFight)
      fights.value.unshift(created)
      Object.assign(newFight, { Name: '', Description: '', Raidplans: [], ImageUrl: '' })
    }
    catch (e: any) {
      error.value = e.message || 'Failed to create fight'
    }
  }

  return { loading, error, fights, newFight, editId, getFights, create }
}
