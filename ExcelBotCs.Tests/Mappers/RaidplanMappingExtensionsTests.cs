using ExcelBotCs.Mappers.Fights;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class RaidplanMappingExtensionsTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var entity = new Raidplan().PopulateWithRandomData();

        var dto = entity.ToDto();

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(entity.Id);
        dto.Name.ShouldBe(entity.Name);
        dto.Description.ShouldBe(entity.Description);
        dto.Url.ShouldBe(entity.Url);
        dto.AuthorId.ShouldBe(entity.AuthorId);
    }

    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var dto = new RaidplanDto().PopulateWithRandomData();

        var entity = dto.ToEntity();

        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(dto.Id);
        entity.Name.ShouldBe(dto.Name);
        entity.Description.ShouldBe(dto.Description);
        entity.Url.ShouldBe(dto.Url);
        entity.AuthorId.ShouldBe(dto.AuthorId);
    }
}