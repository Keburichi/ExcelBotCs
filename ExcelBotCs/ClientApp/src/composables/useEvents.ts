import type { FCEvent, Role } from '@/features/events/events.types'
import { reactive, ref } from 'vue'
import { EventsApi } from '@/features/events/events.api'

export function useEvents() {
  const loading = ref(false)
  const error = ref('')
  const events = ref<FCEvent[]>([])

  const newEvent = reactive<FCEvent>({
    Name: '',
    Description: '',
    DiscordMessage: '',
    Id: '',
    PictureUrl: '',
    Organizer: '',
    Signups: [],
    Participants: [],
    AvailableForSignup: false,
    StartDate: new Date(),
    Duration: 0,
    MaxNumberOfParticipants: 0,
  })

  const editId = ref<string | null>(null)
  const editBuffer = reactive<FCEvent>({
    Name: '',
    Description: '',
    DiscordMessage: '',
    PictureUrl: '',
    Id: '',
    Organizer: '',
    Signups: [],
    Participants: [],
    AvailableForSignup: false,
    StartDate: new Date(),
    Duration: 0,
    MaxNumberOfParticipants: 0,
  })

  async function load() {
    loading.value = true
    error.value = ''
    try {
      events.value = await EventsApi.list()
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
      const created = await EventsApi.create(newEvent)
      events.value.unshift(created)
      Object.assign(newEvent, { Name: '', PlayerName: '', Subbed: false, LodestoneId: '' })
    }
    catch (e: any) {
      error.value = e.message || 'Failed to create event'
    }
  }

  function startEdit(m: FCEvent) {
    editId.value = m.Id ?? null
    Object.assign(editBuffer, m)
  }

  function cancelEdit() {
    editId.value = null
  }

  async function getEvent(id: string) {
    try {
      return await EventsApi.get(id)
    }
    catch (e: any) {
      error.value = e.message || 'Failed to get event'
      return null
    }
  }

  async function save() {
    if (!editId.value)
      return
    try {
      await EventsApi.update(editId.value, editBuffer)
      const i = events.value.findIndex(x => x.Id === editId.value)
      if (i >= 0)
        events.value[i] = { ...editBuffer, Id: editId.value }
      editId.value = null
    }
    catch (e: any) {
      error.value = e.message || 'Failed to save event'
    }
  }

  async function deleteEvent(event: FCEvent) {
    if (!event)
      return

    try {
      await EventsApi.delete(event.Id)
      events.value = events.value.filter(x => x.Id !== event.Id)
    }
    catch (e: any) {
      error.value = e.message || 'Failed to delete event'
    }
  }

  async function signup(event: FCEvent, role: Role) {
    if (!event)
      return

    try {
      await EventsApi.signUp(event, role)
    }
    catch (e: any) {
      error.value = e.message || 'Failed to signup'
    }
  }

  return {
    loading,
    error,
    events,
    newEvent,
    editId,
    editBuffer,
    load,
    create,
    startEdit,
    cancelEdit,
    save,
    deleteEvent,
    signup,
    getEvent,
  }
}
