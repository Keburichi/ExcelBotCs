export interface Fight {
    Id?: string
    Name: string
    Description: string
    ImageUrl?: string
    Type: FightType
    Raidplans: Raidplan[]

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

export enum FightType {
    Normal,
    Extreme,
    Savage,
    LegacySavage,
    Ultimate,
    Chaotic,
    Unreal,
}

export function fightTypeToString(ft: FightType) {
    return FightType[ft]
}
