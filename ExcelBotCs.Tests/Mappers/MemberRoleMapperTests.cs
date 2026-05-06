using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

[TestFixture]
public class MemberRoleMapperTests
{
    [Test]
    public void ToDto()
    {
        // Arrange
        var entity = new MemberRole().PopulateWithRandomData();

        // Act
        var entityDto = MemberRoleMapper.ToDto(entity);

        // Assert
        Assert.That(entityDto, Is.Not.Null);
        Assert.That(entityDto.Id, Is.EqualTo(entity.Id));
        Assert.That(entityDto.DiscordId, Is.EqualTo(entity.DiscordId));
        Assert.That(entityDto.Name, Is.EqualTo(entity.Name));
        Assert.That(entityDto.IsAdmin, Is.EqualTo(entity.IsAdmin));
        Assert.That(entityDto.IsMember, Is.EqualTo(entity.IsMember));
        Assert.That(entityDto.IsDeveloper, Is.EqualTo(entity.IsDeveloper));
    }

    [Test]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new MemberRoleDto().PopulateWithRandomData();

        // Act
        var entity = MemberRoleMapper.ToEntity(entityDto);

        // Assert
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Id, Is.EqualTo(entityDto.Id));
        Assert.That(entity.DiscordId, Is.EqualTo(entityDto.DiscordId));
        Assert.That(entity.Name, Is.EqualTo(entityDto.Name));
        Assert.That(entity.IsAdmin, Is.EqualTo(entityDto.IsAdmin));
        Assert.That(entity.IsMember, Is.EqualTo(entityDto.IsMember));
        Assert.That(entity.IsDeveloper, Is.EqualTo(entityDto.IsDeveloper));
    }
}