import type { Member } from '@/features/members/members.types'

export interface Fight {
  Id?: string
  Name: string
  Description: string
  ImageUrl?: string
  Expansion: string
  Type: FightType
  Raidplans: Raidplan[]
}

export interface Raidplan {
  Id: string
  Name: string
  Description: string
  Url: string
  Author: Member
}

export enum FightType {
  Normal,
  Extreme,
  Savage,
  LegacySavage,
  Ultimate,
  Chaotic,
}

export function fightTypeToString(ft: FightType) {
  return FightType[ft]
}
