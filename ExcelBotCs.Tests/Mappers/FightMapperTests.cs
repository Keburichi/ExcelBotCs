using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

[TestFixture]
public class FightMapperTests
{
    [Test]
    public void ToDto()
    {
        // Arrange
        var entity = new Fight().PopulateWithRandomData();

        // Act
        var entityDto = FightMapper.ToDto(entity);

        // Assert
        Assert.That(entityDto, Is.Not.Null);
        Assert.That(entityDto.Id, Is.EqualTo(entity.Id));
        Assert.That(entityDto.Name, Is.EqualTo(entity.Name));
        Assert.That(entityDto.Description, Is.EqualTo(entity.Description));
        Assert.That(entityDto.ImageUrl, Is.EqualTo(entity.ImageUrl));
        Assert.That(entityDto.Type, Is.EqualTo(entity.Type));
        Assert.That(entityDto.FFLogsExpansionName, Is.EqualTo(entity.FFLogsExpansionName));
        Assert.That(entityDto.FFLogsZoneName, Is.EqualTo(entity.FFLogsZoneName));
        Assert.That(entityDto.IsFrozen, Is.EqualTo(entity.IsFrozen));
        Assert.That(entityDto.Raidplans?.Count, Is.EqualTo(entity.Raidplans?.Count));
    }

    [Test]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new FightDto().PopulateWithRandomData();

        // Act
        var entity = FightMapper.ToEntity(entityDto);

        // Assert
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Id, Is.EqualTo(entityDto.Id));
        Assert.That(entity.Name, Is.EqualTo(entityDto.Name));
        Assert.That(entity.Description, Is.EqualTo(entityDto.Description));
        Assert.That(entity.ImageUrl, Is.EqualTo(entityDto.ImageUrl));
        Assert.That(entity.Type, Is.EqualTo(entityDto.Type));
        Assert.That(entity.FFLogsExpansionName, Is.EqualTo(entityDto.FFLogsExpansionName));
        Assert.That(entity.FFLogsZoneName, Is.EqualTo(entityDto.FFLogsZoneName));
        Assert.That(entity.IsFrozen, Is.EqualTo(entityDto.IsFrozen));
        Assert.That(entity.Raidplans?.Count, Is.EqualTo(entityDto.Raidplans?.Count));
    }
}