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
}

export interface EventOccurrence {
  Id: string
  OccurrenceDate: Date
  Status: OccurrenceStatus
  DiscordMessageId?: string
  Signups: EventSignup[]
  Participants: EventParticipant[]
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

export function canSignUpForOccurrence(occurrence: EventOccurrence, maxParticipants: number): boolean {
  return (
    occurrence.Status === OccurrenceStatus.Scheduled
    && !isOccurrencePast(occurrence)
    && (occurrence.Participants?.length ?? 0) < maxParticipants
  )
}

export enum SignupType {
  SingleEvent = 0,
  IndependentSignups = 1,
  LockedGroup = 2,
}

export function signupTypeToString(type: SignupType): string {
  switch (type) {
    case SignupType.SingleEvent:
      return 'Single Event'
    case SignupType.IndependentSignups:
      return 'Different Group Each Time'
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

export function eventTypeToBadgeBgColor(type: EventType): string {
  switch (type) {
    case EventType.Raid:
      return '#e3f2fd'
    case EventType.Social:
      return '#f3e5f5'
    case EventType.Farming:
      return '#e8f5e9'
    case EventType.Maps:
      return '#fff3e0'
    case EventType.BLU:
      return '#e0f2f1'
    case EventType.Academy:
      return '#fce4ec'
    case EventType.MinIlvl:
      return '#fff9c4'
    case EventType.Downsynced:
      return '#ede7f6'
    case EventType.Other:
    default:
      return '#eceff1'
  }
}
