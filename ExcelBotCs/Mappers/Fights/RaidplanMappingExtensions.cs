using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers.Fights;

public static class RaidplanMappingExtensions
{
    public static RaidplanDto ToDto(this Raidplan raidplan)
    {
        return new RaidplanDto
        {
            Id = raidplan.Id,
            Name = raidplan.Name,
            Description = raidplan.Description,
            Url = raidplan.Url,
            AuthorId = raidplan.AuthorId
        };
    }

    public static Raidplan ToEntity(this RaidplanDto raidplan)
    {
        return new Raidplan
        {
            Id = raidplan.Id,
            Name = raidplan.Name,
            Description = raidplan.Description,
            Url = raidplan.Url,
            AuthorId = raidplan.AuthorId
        };
    }
}