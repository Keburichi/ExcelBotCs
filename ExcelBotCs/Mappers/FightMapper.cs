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
            Raidplans = fight.Raidplans,
            FFLogsExpansionName = fight.FFLogsExpansionName,
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
            Raidplans = fight.Raidplans,
            FFLogsExpansionName = fight.FFLogsExpansionName,
        };
    }
}