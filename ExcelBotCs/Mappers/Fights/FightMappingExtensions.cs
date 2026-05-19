using ExcelBotCs.Mappers.Resources;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.Models.DTO.Fights;
using ExcelBotCs.Models.DTO.Resources;

namespace ExcelBotCs.Mappers.Fights;

public static class FightMappingExtensions
{
    public static FightResponse ToFightResponse(this Fight fight, Boss? boss = null, List<Resource>? resources = null)
    {
        return new FightResponse
        {
            Id = fight.Id,
            Name = fight.Name,
            Type = fight.Type,
            BossId = fight.BossId,
            BossName = boss?.Name,
            ImageUrl = boss?.ImageUrl,
            FFLogsEncounterId = fight.FFLogsEncounterId,
            FFLogsZoneId = fight.FFLogsZoneId,
            FFLogsZoneName = fight.FFLogsZoneName,
            FFLogsExpansionId = fight.FFLogsExpansionId,
            FFLogsExpansionName = fight.FFLogsExpansionName,
            IsFrozen = fight.IsFrozen,
            Resources = resources?.Select(r => r.ToResourceResponse()).ToList() ?? new()
        };
    }

    public static Fight ToEntity(this CreateFightRequest request)
    {
        return new Fight
        {
            Name = request.Name,
            Type = request.Type,
            BossId = request.BossId,
            FFLogsEncounterId = request.FFLogsEncounterId,
            FFLogsZoneId = request.FFLogsZoneId,
            FFLogsZoneName = request.FFLogsZoneName,
            FFLogsDifficultyId = request.FFLogsDifficultyId,
            FFLogsExpansionId = request.FFLogsExpansionId,
            FFLogsExpansionName = request.FFLogsExpansionName
        };
    }

    public static void ApplyUpdate(this Fight fight, UpdateFightRequest request)
    {
        if (request.Name != null)
            fight.Name = request.Name;

        if (request.Type.HasValue)
            fight.Type = request.Type.Value;

        if (request.BossId != null)
            fight.BossId = request.BossId;
    }

    // Legacy mapping kept for backward compatibility with other services still using FightDto
    public static FightDto ToDto(this Fight fight)
    {
        return new FightDto
        {
            Id = fight.Id,
            Name = fight.Name,
            Type = fight.Type,
            Raidplans = fight.Raidplans?.Select(r => r.ToDto()).ToList(),
            FFLogsExpansionName = fight.FFLogsExpansionName,
            FFLogsZoneName = fight.FFLogsZoneName,
            IsFrozen = fight.IsFrozen
        };
    }

    public static Fight ToEntity(this FightDto fight)
    {
        return new Fight
        {
            Id = fight.Id,
            Name = fight.Name,
            Type = fight.Type,
            Raidplans = fight.Raidplans?.Select(r => r.ToEntity()).ToList(),
            FFLogsExpansionName = fight.FFLogsExpansionName,
            FFLogsZoneName = fight.FFLogsZoneName,
            IsFrozen = fight.IsFrozen
        };
    }
}
