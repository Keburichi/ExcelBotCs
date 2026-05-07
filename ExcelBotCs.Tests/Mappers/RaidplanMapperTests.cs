using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class RaidplanMapperTests
{
    [Fact]
    public void ToDto()
    {
        // Arrange
        var entity = new Raidplan().PopulateWithRandomData();

        // Act
        var entityDto = RaidplanMapper.ToDto(entity);

        // Assert
        entityDto.ShouldNotBeNull();
        entityDto.Id.ShouldBe(entity.Id);
        entityDto.Name.ShouldBe(entity.Name);
        entityDto.Description.ShouldBe(entity.Description);
        entityDto.Url.ShouldBe(entity.Url);
        entityDto.AuthorId.ShouldBe(entity.AuthorId);
    }

    [Fact]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new RaidplanDto().PopulateWithRandomData();

        // Act
        var entity = RaidplanMapper.ToEntity(entityDto);

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(entityDto.Id);
        entity.Name.ShouldBe(entityDto.Name);
        entity.Description.ShouldBe(entityDto.Description);
        entity.Url.ShouldBe(entityDto.Url);
        entity.AuthorId.ShouldBe(entityDto.AuthorId);
    }
}