import type { EventType, SignupButtonConfig } from '@/features/events/events.types'

export interface EventTemplate {
  Id: string
  Name: string
  Description: string
  Type: EventType
  DayOfWeek: DayOfWeek
  TimeOfDayMinutes: number
  Duration: number
  Organizer: string
  MaxNumberOfParticipants: number
  SignupButtonConfigs?: SignupButtonConfig[]
}

export enum DayOfWeek {
  Sunday = 0,
  Monday = 1,
  Tuesday = 2,
  Wednesday = 3,
  Thursday = 4,
  Friday = 5,
  Saturday = 6,
}

export function dayOfWeekToString(day: DayOfWeek): string {
  return DayOfWeek[day]
}

export function formatTimeOfDay(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}
