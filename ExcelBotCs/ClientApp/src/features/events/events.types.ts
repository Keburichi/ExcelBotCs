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
}

export interface EventParticipant{
    DiscordUserId: number,
    Role: Role
}

export interface EventSignup{
    DiscordUserId: number,
    Role: Role
}

export enum Role{
    Tank,
    Healer,
    Melee,
    Caster,
    Ranged
}