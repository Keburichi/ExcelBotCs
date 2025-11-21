using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

[TestFixture]
public class MemberMapperTests
{
    [Test]
    public void ToDto()
    {
        // Arrange
        var entity = new Member().PopulateWithRandomData();

        // Act
        var entityDto = MemberMapper.ToDto(entity);

        // Assert
        Assert.That(entityDto, Is.Not.Null);
        Assert.That(entityDto.Id, Is.EqualTo(entity.Id));
        Assert.That(entityDto.DiscordId, Is.EqualTo(entity.DiscordId));
        Assert.That(entityDto.DiscordAvatar, Is.EqualTo(entity.DiscordAvatar));
        Assert.That(entityDto.DiscordName, Is.EqualTo(entity.DiscordName));
        Assert.That(entityDto.LodestoneId, Is.EqualTo(entity.LodestoneId));
        Assert.That(entityDto.LodestoneVerificationToken, Is.EqualTo(entity.LodestoneVerificationToken));
        Assert.That(entityDto.PlayerName, Is.EqualTo(entity.PlayerName));
        Assert.That(entityDto.Subbed, Is.EqualTo(entity.Subbed));
        Assert.That(entityDto.Experience?.Count, Is.EqualTo(entity.Experience?.Count));
        Assert.That(entityDto.Notes?.Count, Is.EqualTo(entity.Notes?.Count));
        Assert.That(entityDto.Roles.Count, Is.EqualTo(entity.Roles.Count));
    }

    [Test]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new MemberDto().PopulateWithRandomData();

        // Act
        var entity = MemberMapper.ToEntity(entityDto);

        // Assert
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Id, Is.EqualTo(entityDto.Id));
        Assert.That(entity.DiscordId, Is.EqualTo(entityDto.DiscordId));
        Assert.That(entity.DiscordAvatar, Is.EqualTo(entityDto.DiscordAvatar));
        Assert.That(entity.DiscordName, Is.EqualTo(entityDto.DiscordName));
        Assert.That(entity.LodestoneId, Is.EqualTo(entityDto.LodestoneId));
        Assert.That(entity.LodestoneVerificationToken, Is.EqualTo(entityDto.LodestoneVerificationToken));
        Assert.That(entity.PlayerName, Is.EqualTo(entityDto.PlayerName));
        Assert.That(entity.Subbed, Is.EqualTo(entityDto.Subbed));
        Assert.That(entity.Experience?.Count, Is.EqualTo(entityDto.Experience?.Count));
        Assert.That(entity.Notes?.Count, Is.EqualTo(entityDto.Notes?.Count));
        Assert.That(entity.Roles.Count, Is.EqualTo(entityDto.Roles.Count));
    }
}