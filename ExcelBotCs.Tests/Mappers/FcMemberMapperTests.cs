using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

[TestFixture]
public class FcMemberMapperTests
{
    [Test]
    public void ToDto()
    {
        // Arrange
        var entity = new FcMember().PopulateWithRandomData();

        // Act
        var entityDto = FcMemberMapper.ToDto(entity);

        // Assert
        Assert.That(entityDto, Is.Not.Null);
        Assert.That(entityDto.Id, Is.EqualTo(entity.Id));
        Assert.That(entityDto.Name, Is.EqualTo(entity.Name));
        Assert.That(entityDto.Bio, Is.EqualTo(entity.Bio));
        Assert.That(entityDto.Avatar, Is.EqualTo(entity.Avatar));
        Assert.That(entityDto.CharacterId, Is.EqualTo(entity.CharacterId));
        Assert.That(entityDto.FcRank, Is.EqualTo(entity.FcRank));
        Assert.That(entityDto.Title, Is.EqualTo(entity.Title));
        Assert.That(entityDto.LastSynchronisation, Is.EqualTo(entity.LastSynchronisation));
    }

    [Test]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new FcMemberDto().PopulateWithRandomData();

        // Act
        var entity = FcMemberMapper.ToEntity(entityDto);

        // Assert
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Id, Is.EqualTo(entityDto.Id));
        Assert.That(entity.Name, Is.EqualTo(entityDto.Name));
        Assert.That(entity.Bio, Is.EqualTo(entityDto.Bio));
        Assert.That(entity.Avatar, Is.EqualTo(entityDto.Avatar));
        Assert.That(entity.CharacterId, Is.EqualTo(entityDto.CharacterId));
        Assert.That(entity.FcRank, Is.EqualTo(entityDto.FcRank));
        Assert.That(entity.Title, Is.EqualTo(entityDto.Title));
        Assert.That(entity.LastSynchronisation, Is.EqualTo(entityDto.LastSynchronisation));
    }
}