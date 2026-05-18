using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers.Fights;

public static class FightMappingExtensions
{
    public static FightDto ToDto(this Fight fight)
    {
        return new FightDto
        {
            Id = fight.Id,
            Name = fight.Name,
            Description = fight.Description,
            ImageUrl = fight.ImageUrl,
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
            Description = fight.Description,
            ImageUrl = fight.ImageUrl,
            Type = fight.Type,
            Raidplans = fight.Raidplans?.Select(r => r.ToEntity()).ToList(),
            FFLogsExpansionName = fight.FFLogsExpansionName,
            FFLogsZoneName = fight.FFLogsZoneName,
            IsFrozen = fight.IsFrozen
        };
    }
}