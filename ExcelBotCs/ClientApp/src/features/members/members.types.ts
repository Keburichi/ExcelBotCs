import type { Fight } from '@/features/fights/fights.types'

export interface Member {
  Id?: string
  DiscordId: string
  DiscordName: string
  DiscordAvatar: string
  PlayerName: string
  Subbed: boolean
  LodestoneId: string
  LodestoneVerificationToken?: string
  Experience: Fight[]
  Notes: MemberNote[]
  Roles: MemberRole[]
  IsAdmin: boolean
  IsMember: boolean
}

export interface MemberNote {
  Id: string
  Note: string
  Author: Member
}

export interface MemberRole {
  Id?: string
  Name: string
  DiscordId: string
  IsAdmin: boolean
  IsMember: boolean
}
