using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class FightMapper
{
    public static FightDto ToDto(Fight fight)
    {
        return new FightDto()
        {
            Id = fight.Id,
            Name = fight.Name,
            Description = fight.Description,
            ImageUrl = fight.ImageUrl,
            Type = fight.Type,
            Raidplans = fight.Raidplans?.Select(RaidplanMapper.ToDto).ToList(),
            FFLogsExpansionName = fight.FFLogsExpansionName,
            FFLogsZoneName = fight.FFLogsZoneName,
            IsFrozen = fight.IsFrozen
        };
    }

    public static Fight ToEntity(FightDto fight)
    {
        return new Fight()
        {
            Id = fight.Id,
            Name = fight.Name,
            Description = fight.Description,
            ImageUrl = fight.ImageUrl,
            Type = fight.Type,
            Raidplans = fight.Raidplans?.Select(RaidplanMapper.ToEntity).ToList(),
            FFLogsExpansionName = fight.FFLogsExpansionName,
            FFLogsZoneName = fight.FFLogsZoneName,
            IsFrozen = fight.IsFrozen
        };
    }
}