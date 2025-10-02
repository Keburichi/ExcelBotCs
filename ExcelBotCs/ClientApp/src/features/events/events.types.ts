export interface FCEvent {
    Id: string
    Name: string
    Description: string
    DiscordMessage: string
    PictureUrl?: string
    Participants: EventParticipant[]
    Signups: EventSignup[]
    Organizer: string
    StartDate: Date
    Duration: number,
    AvailableForSignup: boolean
    MaxNumberOfParticipants: number
}

export interface EventParticipant{
    DiscordUserId: string,
    Role: Role
}

export interface EventSignup{
    DiscordUserId: string,
    Roles: Role[]
}

export const ROLE = {
    Tank: 0,
    Healer: 1,
    Melee: 2,
    Caster: 3,
    Ranged: 4
} as const

export type Role = (typeof ROLE)[keyof typeof ROLE]