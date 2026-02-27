import type {
  ArchiveSearchParams,
  EventOccurrence,
  EventParticipant,
  FCEvent,
  OccurrenceStatus,
  Role,
} from '@/features/events/events.types'
import { reactive, ref } from 'vue'
import { EventsApi } from '@/features/events/events.api'
import { canSignUpForOccurrence, isOccurrencePast, OccurrenceStatus as OccStatus } from '@/features/events/events.types'

export function useEvents() {
  const loading = ref(false)
  const error = ref('')
  const events = ref<FCEvent[]>([])
  const archivedEvents = ref<FCEvent[]>([])
  const archiveLoading = ref(false)

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
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed'
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
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to create event'
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
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to get event'
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
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to save event'
    }
  }

  async function deleteEvent(event: FCEvent) {
    if (!event)
      return

    try {
      await EventsApi.delete(event.Id)
      events.value = events.value.filter(x => x.Id !== event.Id)
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to delete event'
    }
  }

  async function signup(event: FCEvent, role: Role) {
    if (!event)
      return

    try {
      await EventsApi.signUp(event, role)
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to signup'
    }
  }

  // Occurrence-specific methods
  async function signUpForOccurrence(eventId: string, occurrenceId: string, roles: Role[]) {
    try {
      await EventsApi.signUpForOccurrence(eventId, occurrenceId, roles)
      await load() // Reload events to get updated data
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to sign up for occurrence'
    }
  }

  async function cancelSignupForOccurrence(eventId: string, occurrenceId: string) {
    try {
      await EventsApi.cancelSignup(eventId, occurrenceId)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to cancel signup'
    }
  }

  async function selectParticipantsForOccurrence(eventId: string, occurrenceId: string, participants: EventParticipant[]) {
    try {
      await EventsApi.selectParticipants(eventId, occurrenceId, participants)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to select participants'
    }
  }

  async function removeParticipantFromOccurrence(eventId: string, occurrenceId: string, userId: string) {
    try {
      await EventsApi.removeParticipant(eventId, occurrenceId, userId)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to remove participant'
    }
  }

  async function updateOccurrenceStatusById(eventId: string, occurrenceId: string, status: OccurrenceStatus) {
    try {
      await EventsApi.updateOccurrenceStatus(eventId, occurrenceId, status)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to update occurrence status'
      throw e // Re-throw so callers can handle it
    }
  }

  async function cancelOccurrenceById(eventId: string, occurrenceId: string) {
    try {
      await EventsApi.cancelOccurrence(eventId, occurrenceId)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to cancel occurrence'
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

  /**
   * Gets the next occurrence that can be concluded.
   * Prioritizes past scheduled occurrences (since you can only complete past events),
   * then falls back to the next upcoming scheduled occurrence.
   */
  function getOccurrenceToComplete(event: FCEvent): EventOccurrence | null {
    if (!event.Occurrences)
      return null

    // First, look for past scheduled occurrences (these need to be concluded)
    const pastScheduled = event.Occurrences
      .filter(o => o.Status === OccStatus.Scheduled && isOccurrencePast(o))
      .sort((a, b) => new Date(a.OccurrenceDate).getTime() - new Date(b.OccurrenceDate).getTime())

    if (pastScheduled.length > 0)
      return pastScheduled[0]

    // Fall back to next upcoming occurrence (for display purposes, though it can't be completed yet)
    return getNextOccurrence(event)
  }

  function canUserSignUp(event: FCEvent, occurrence: EventOccurrence): boolean {
    return canSignUpForOccurrence(occurrence, event.MaxNumberOfParticipants)
  }

  // Archive/Restore methods
  async function loadArchived(searchParams?: ArchiveSearchParams) {
    archiveLoading.value = true
    error.value = ''
    try {
      archivedEvents.value = await EventsApi.listArchived(searchParams)
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to load archived events'
    }
    finally {
      archiveLoading.value = false
    }
  }

  async function archiveEvent(eventId: string) {
    try {
      await EventsApi.archive(eventId)
      // Remove from active events
      events.value = events.value.filter(e => e.Id !== eventId)
      return true
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to archive event'
      return false
    }
  }

  async function restoreEvent(eventId: string) {
    try {
      await EventsApi.restore(eventId)
      // Remove from archived events
      archivedEvents.value = archivedEvents.value.filter(e => e.Id !== eventId)
      // Reload active events to include the restored one
      await load()
      return true
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to restore event'
      return false
    }
  }

  async function extendEvent(eventId: string, count: number) {
    try {
      const updatedEvent = await EventsApi.extend(eventId, { Count: count })
      // Update event in the list
      const index = events.value.findIndex(e => e.Id === eventId)
      if (index >= 0) {
        events.value[index] = updatedEvent
      }
      return updatedEvent
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to extend event'
      return null
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
    getOccurrenceToComplete,
    canUserSignUp,
    // Archive/Restore methods
    archivedEvents,
    archiveLoading,
    loadArchived,
    archiveEvent,
    restoreEvent,
    extendEvent,
  }
}
