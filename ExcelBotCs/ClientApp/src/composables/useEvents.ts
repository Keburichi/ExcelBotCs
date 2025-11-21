import type { EventOccurrence, EventParticipant, FCEvent, OccurrenceStatus, Role } from '@/features/events/events.types'
import { reactive, ref } from 'vue'
import { EventsApi } from '@/features/events/events.api'
import { canSignUpForOccurrence, isOccurrencePast, OccurrenceStatus as OccStatus } from '@/features/events/events.types'

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
    Occurrences: [],
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
    Occurrences: [],
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

  // Occurrence-specific methods
  async function signUpForOccurrence(eventId: string, occurrenceId: string, roles: Role[]) {
    try {
      await EventsApi.signUpForOccurrence(eventId, occurrenceId, roles)
      await load() // Reload events to get updated data
    }
    catch (e: any) {
      error.value = e.message || 'Failed to sign up for occurrence'
    }
  }

  async function cancelSignupForOccurrence(eventId: string, occurrenceId: string) {
    try {
      await EventsApi.cancelSignup(eventId, occurrenceId)
      await load()
    }
    catch (e: any) {
      error.value = e.message || 'Failed to cancel signup'
    }
  }

  async function selectParticipantsForOccurrence(eventId: string, occurrenceId: string, participants: EventParticipant[]) {
    try {
      await EventsApi.selectParticipants(eventId, occurrenceId, participants)
      await load()
    }
    catch (e: any) {
      error.value = e.message || 'Failed to select participants'
    }
  }

  async function removeParticipantFromOccurrence(eventId: string, occurrenceId: string, userId: string) {
    try {
      await EventsApi.removeParticipant(eventId, occurrenceId, userId)
      await load()
    }
    catch (e: any) {
      error.value = e.message || 'Failed to remove participant'
    }
  }

  async function updateOccurrenceStatusById(eventId: string, occurrenceId: string, status: OccurrenceStatus) {
    try {
      await EventsApi.updateOccurrenceStatus(eventId, occurrenceId, status)
      await load()
    }
    catch (e: any) {
      error.value = e.message || 'Failed to update occurrence status'
    }
  }

  async function cancelOccurrenceById(eventId: string, occurrenceId: string) {
    try {
      await EventsApi.cancelOccurrence(eventId, occurrenceId)
      await load()
    }
    catch (e: any) {
      error.value = e.message || 'Failed to cancel occurrence'
    }
  }

  // Helper methods
  function getUpcomingOccurrences(event: FCEvent): EventOccurrence[] {
    if (!event.Occurrences)
      return []
    return event.Occurrences
      .filter(o => o.Status === OccStatus.Scheduled && !isOccurrencePast(o))
      .sort((a, b) => new Date(a.OccurrenceDate).getTime() - new Date(b.OccurrenceDate).getTime())
  }

  function getNextOccurrence(event: FCEvent): EventOccurrence | null {
    const upcoming = getUpcomingOccurrences(event)
    return upcoming.length > 0 ? upcoming[0] : null
  }

  function canUserSignUp(event: FCEvent, occurrence: EventOccurrence): boolean {
    return canSignUpForOccurrence(occurrence, event.MaxNumberOfParticipants)
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
    // Occurrence-specific methods
    signUpForOccurrence,
    cancelSignupForOccurrence,
    selectParticipantsForOccurrence,
    removeParticipantFromOccurrence,
    updateOccurrenceStatusById,
    cancelOccurrenceById,
    // Helper methods
    getUpcomingOccurrences,
    getNextOccurrence,
    canUserSignUp,
  }
}
