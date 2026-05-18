using ExcelBotCs.Mappers.Fights;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class FightMappingExtensionsTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var entity = new Fight().PopulateWithRandomData();

        var dto = entity.ToDto();

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(entity.Id);
        dto.Name.ShouldBe(entity.Name);
        dto.Description.ShouldBe(entity.Description);
        dto.ImageUrl.ShouldBe(entity.ImageUrl);
        dto.Type.ShouldBe(entity.Type);
        dto.FFLogsExpansionName.ShouldBe(entity.FFLogsExpansionName);
        dto.FFLogsZoneName.ShouldBe(entity.FFLogsZoneName);
        dto.IsFrozen.ShouldBe(entity.IsFrozen);
        (dto.Raidplans?.Count ?? 0).ShouldBe(entity.Raidplans?.Count ?? 0);
    }

    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var dto = new FightDto().PopulateWithRandomData();

        var entity = dto.ToEntity();

        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(dto.Id);
        entity.Name.ShouldBe(dto.Name);
        entity.Description.ShouldBe(dto.Description);
        entity.ImageUrl.ShouldBe(dto.ImageUrl);
        entity.Type.ShouldBe(dto.Type);
        entity.FFLogsExpansionName.ShouldBe(dto.FFLogsExpansionName);
        entity.FFLogsZoneName.ShouldBe(dto.FFLogsZoneName);
        entity.IsFrozen.ShouldBe(dto.IsFrozen);
        (entity.Raidplans?.Count ?? 0).ShouldBe(dto.Raidplans?.Count ?? 0);
    }
}