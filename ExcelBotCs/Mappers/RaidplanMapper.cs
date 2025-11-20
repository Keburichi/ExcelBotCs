using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class RaidplanMapper
{
    public static RaidplanDto ToDto(Raidplan raidplan)
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

    public static Raidplan ToEntity(RaidplanDto raidplan)
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