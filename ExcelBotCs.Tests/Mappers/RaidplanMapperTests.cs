using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

[TestFixture]
public class RaidplanMapperTests
{
    [Test]
    public void ToDto()
    {
        // Arrange
        var entity = new Raidplan().PopulateWithRandomData();

        // Act
        var entityDto = RaidplanMapper.ToDto(entity);

        // Assert
        Assert.That(entityDto, Is.Not.Null);
        Assert.That(entityDto.Id, Is.EqualTo(entity.Id));
        Assert.That(entityDto.Name, Is.EqualTo(entity.Name));
        Assert.That(entityDto.Description, Is.EqualTo(entity.Description));
        Assert.That(entityDto.Url, Is.EqualTo(entity.Url));
        Assert.That(entityDto.AuthorId, Is.EqualTo(entity.AuthorId));
    }

    [Test]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new RaidplanDto().PopulateWithRandomData();

        // Act
        var entity = RaidplanMapper.ToEntity(entityDto);

        // Assert
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Id, Is.EqualTo(entityDto.Id));
        Assert.That(entity.Name, Is.EqualTo(entityDto.Name));
        Assert.That(entity.Description, Is.EqualTo(entityDto.Description));
        Assert.That(entity.Url, Is.EqualTo(entityDto.Url));
        Assert.That(entity.AuthorId, Is.EqualTo(entityDto.AuthorId));
    }
}