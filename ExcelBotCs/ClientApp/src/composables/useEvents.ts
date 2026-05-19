import type {
  ArchiveSearchParams,
  EventGroupRequest,
  EventOccurrence,
  FCEvent,
  OccurrenceStatus,
  Role,
} from '@/features/events/events.types'
import { reactive, ref } from 'vue'
import { EventsApi } from '@/features/events/events.api'
import { EventType, isOccurrencePast, OccurrenceStatus as OccStatus, SignupType } from '@/features/events/events.types'

export function useEvents() {
  const loading = ref(false)
  const error = ref('')
  const events = ref<FCEvent[]>([])
  const archivedEvents = ref<FCEvent[]>([])
  const archiveLoading = ref(false)

  // Archive pagination state
  const archivePage = ref(1)
  const archivePageSize = ref(20)
  const archiveTotalCount = ref(0)
  const archiveHasMore = ref(false)

  const newEvent = reactive<FCEvent>({
    Id: '',
    Name: '',
    Description: '',
    Type: 0 as EventType,
    StartDate: new Date(),
    EndDate: new Date(),
    Duration: 0,
    ICalString: '',
    SignupType: 0 as SignupType,
    DiscordMessageId: '',
    PictureUrl: '',
    Organizer: '',
    Occurrences: [],
    Signups: [],
    Groups: [],
    AvailableForSignup: false,
    MaxNumberOfParticipants: 0,
    RequiredParticipants: 0,
    IsArchived: false,
    CanBeArchived: false,
  })

  const editId = ref<string | null>(null)
  const editBuffer = reactive<FCEvent>({
    Id: '',
    Name: '',
    Description: '',
    Type: 0 as EventType,
    StartDate: new Date(),
    EndDate: new Date(),
    Duration: 0,
    ICalString: '',
    SignupType: 0 as SignupType,
    DiscordMessageId: '',
    PictureUrl: '',
    Organizer: '',
    Occurrences: [],
    Signups: [],
    Groups: [],
    AvailableForSignup: false,
    MaxNumberOfParticipants: 0,
    RequiredParticipants: 0,
    IsArchived: false,
    CanBeArchived: false,
  })

  async function load() {
    loading.value = true
    error.value = ''
    try {
      const allEvents: FCEvent[] = []
      let page = 1
      const pageSize = 50
      let hasMore = true
      while (hasMore) {
        const result = await EventsApi.list(page, pageSize)
        allEvents.push(...result.Items)
        hasMore = result.HasMore
        page++
      }
      events.value = allEvents
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

  // Event-level signup
  async function signUpForEvent(eventId: string, roles: Role[]) {
    try {
      await EventsApi.signUp(eventId, roles)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to sign up for event'
    }
  }

  async function signUpWithSlugs(eventId: string, slugs: string[]) {
    try {
      await EventsApi.signUpWithSlugs(eventId, slugs)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to sign up for event'
    }
  }

  async function cancelSignupForEvent(eventId: string) {
    try {
      await EventsApi.cancelSignup(eventId)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to cancel signup'
    }
  }

  // Group-based participant selection
  async function selectParticipantsForEvent(eventId: string, groups: EventGroupRequest[]) {
    try {
      await EventsApi.selectParticipants(eventId, groups)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to select participants'
    }
  }

  async function removeParticipantFromEvent(eventId: string, userId: string) {
    try {
      await EventsApi.removeParticipant(eventId, userId)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to remove participant'
    }
  }

  // Occurrence management
  async function updateOccurrenceStatusById(eventId: string, occurrenceId: string, status: OccurrenceStatus) {
    try {
      await EventsApi.updateOccurrenceStatus(eventId, occurrenceId, status)
      await load()
    }
    catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to update occurrence status'
      throw e
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

  function getOccurrenceToComplete(event: FCEvent): EventOccurrence | null {
    if (!event.Occurrences)
      return null

    const pastScheduled = event.Occurrences
      .filter(o => o.Status === OccStatus.Scheduled && isOccurrencePast(o))
      .sort((a, b) => new Date(a.OccurrenceDate).getTime() - new Date(b.OccurrenceDate).getTime())

    if (pastScheduled.length > 0)
      return pastScheduled[0]

    return getNextOccurrence(event)
  }

  // Archive/Restore methods
  async function loadArchived(searchParams?: ArchiveSearchParams, page?: number, pageSize?: number) {
    archiveLoading.value = true
    error.value = ''
    if (page !== undefined)
      archivePage.value = page
    if (pageSize !== undefined)
      archivePageSize.value = pageSize
    try {
      const result = await EventsApi.listArchived(archivePage.value, archivePageSize.value, searchParams)
      archivedEvents.value = result.Items
      archiveTotalCount.value = result.TotalCount
      archiveHasMore.value = result.HasMore
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
      archivedEvents.value = archivedEvents.value.filter(e => e.Id !== eventId)
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
    getEvent,
    // Event-level signup/participant methods
    signUpForEvent,
    signUpWithSlugs,
    cancelSignupForEvent,
    selectParticipantsForEvent,
    removeParticipantFromEvent,
    // Occurrence methods
    updateOccurrenceStatusById,
    cancelOccurrenceById,
    // Helper methods
    getUpcomingOccurrences,
    getNextOccurrence,
    getOccurrenceToComplete,
    // Archive/Restore methods
    archivedEvents,
    archiveLoading,
    archivePage,
    archivePageSize,
    archiveTotalCount,
    archiveHasMore,
    loadArchived,
    archiveEvent,
    restoreEvent,
    extendEvent,
  }
}
