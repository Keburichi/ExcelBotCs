export interface Fight {
  Id?: string
  Name: string
  ImageUrl?: string
  Type: FightType
  BossId?: string
  BossName?: string
  Raidplans?: Raidplan[]
  Resources?: Resource[]

  // FFLogs Integration Fields
  FFLogsZoneName?: string
  FFLogsExpansionName?: string
  IsFrozen?: boolean
}

export interface Raidplan {
  Id?: string
  Name: string
  Description: string
  Url: string
  AuthorId: string
}

export interface Resource {
  Id?: string
  Name: string
  Description?: string
  Url: string
  Type: ResourceType
  FightId?: string
  AuthorId: string
}

export enum ResourceType {
  Raidplan = 0,
  VideoGuide = 1,
  Macro = 2,
  Waymarks = 3,
  GeneralLink = 4,
}

export const ResourceTypeLabels: Record<ResourceType, string> = {
  [ResourceType.Raidplan]: 'Raidplan',
  [ResourceType.VideoGuide]: 'Video Guide',
  [ResourceType.Macro]: 'Macro',
  [ResourceType.Waymarks]: 'Waymarks',
  [ResourceType.GeneralLink]: 'Link',
}

export enum FightType {
  Normal,
  Extreme,
  Savage,
  Ultimate,
  Chaotic,
  Unreal,
}

export function fightTypeToString(ft: FightType) {
  return FightType[ft]
}
