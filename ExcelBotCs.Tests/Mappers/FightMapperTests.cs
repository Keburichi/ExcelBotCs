using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class FightMapperTests
{
    [Fact]
    public void ToDto()
    {
        // Arrange
        var entity = new Fight().PopulateWithRandomData();

        // Act
        var entityDto = FightMapper.ToDto(entity);

        // Assert
        entityDto.ShouldNotBeNull();
        entityDto.Id.ShouldBe(entity.Id);
        entityDto.Name.ShouldBe(entity.Name);
        entityDto.Description.ShouldBe(entity.Description);
        entityDto.ImageUrl.ShouldBe(entity.ImageUrl);
        entityDto.Type.ShouldBe(entity.Type);
        entityDto.FFLogsExpansionName.ShouldBe(entity.FFLogsExpansionName);
        entityDto.FFLogsZoneName.ShouldBe(entity.FFLogsZoneName);
        entityDto.IsFrozen.ShouldBe(entity.IsFrozen);
        (entityDto.Raidplans?.Count ?? 0).ShouldBe(entity.Raidplans?.Count ?? 0);
    }

    [Fact]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new FightDto().PopulateWithRandomData();

        // Act
        var entity = FightMapper.ToEntity(entityDto);

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(entityDto.Id);
        entity.Name.ShouldBe(entityDto.Name);
        entity.Description.ShouldBe(entityDto.Description);
        entity.ImageUrl.ShouldBe(entityDto.ImageUrl);
        entity.Type.ShouldBe(entityDto.Type);
        entity.FFLogsExpansionName.ShouldBe(entityDto.FFLogsExpansionName);
        entity.FFLogsZoneName.ShouldBe(entityDto.FFLogsZoneName);
        entity.IsFrozen.ShouldBe(entityDto.IsFrozen);
        (entity.Raidplans?.Count ?? 0).ShouldBe(entityDto.Raidplans?.Count ?? 0);
    }
}