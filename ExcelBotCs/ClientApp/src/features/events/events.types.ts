export interface PagedResult<T> {
  Items: T[]
  TotalCount: number
  Page: number
  PageSize: number
  HasMore: boolean
}

export interface FCEvent {
  Id: string
  Name: string
  Description: string
  Type: EventType
  StartDate: Date
  EndDate: Date
  Duration: number
  ICalString: string
  SignupType: SignupType
  DiscordMessageId: string
  PictureUrl?: string
  FightId?: string
  Occurrences: EventOccurrence[]
  AuthorId?: string
  Organizer: string
  AvailableForSignup: boolean
  MaxNumberOfParticipants: number
  Signups: EventSignup[]
  Groups: EventGroupResponse[]
  // Archive properties
  IsArchived: boolean
  ArchivedDate?: string
  ArchivedByUserId?: string
  CanBeArchived: boolean
}

export interface ArchiveSearchParams {
  searchText?: string
  startDate?: string
  endDate?: string
  eventType?: EventType
}

export interface ExtendEventRequest {
  Count: number
}

export interface EventOccurrence {
  Id: string
  OccurrenceDate: Date
  Status: OccurrenceStatus
}

export interface EventParticipant {
  DiscordUserId: string
  Role: Role
  SelectionDate: Date
}

export interface EventSignup {
  DiscordUserId: string
  Roles: Role[]
  SignupDate: Date
}

export interface EventGroupResponse {
  Id: string
  Name: string
  Participants: EventParticipant[]
}

export interface EventGroupRequest {
  Id?: string
  Name: string
  Participants: EventParticipant[]
}

export enum OccurrenceStatus {
  Scheduled = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3,
}

export function occurrenceStatusToString(status: OccurrenceStatus): string {
  switch (status) {
    case OccurrenceStatus.Scheduled:
      return 'Scheduled'
    case OccurrenceStatus.InProgress:
      return 'In Progress'
    case OccurrenceStatus.Completed:
      return 'Completed'
    case OccurrenceStatus.Cancelled:
      return 'Cancelled'
    default:
      return 'Unknown'
  }
}

export function isOccurrencePast(occurrence: EventOccurrence): boolean {
  return new Date(occurrence.OccurrenceDate) < new Date()
}

export enum SignupType {
  SingleEvent = 0,
  LockedGroup = 2,
}

export function signupTypeToString(type: SignupType): string {
  switch (type) {
    case SignupType.SingleEvent:
      return 'Single Event'
    case SignupType.LockedGroup:
      return 'Same Group All Occurrences'
    default:
      return 'Unknown'
  }
}

export const ROLE = {
  Tank: 0,
  Healer: 1,
  Melee: 2,
  Caster: 3,
  Ranged: 4,
} as const

export type Role = (typeof ROLE)[keyof typeof ROLE]

export enum EventType {
  Raid,
  Social,
  Farming,
  Maps,
  BLU,
  Academy,
  MinIlvl,
  Downsynced,
  Other,
}

export function eventTypeToString(type: EventType): string {
  return EventType[type]
}

