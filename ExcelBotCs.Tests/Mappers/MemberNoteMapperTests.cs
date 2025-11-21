using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

[TestFixture]
public class MemberNoteMapperTests
{
    [Test]
    public void ToDto()
    {
        // Arrange
        var entity = new MemberNote().PopulateWithRandomData();

        // Act
        var entityDto = MemberNoteMapper.ToDto(entity);

        // Assert
        Assert.That(entityDto, Is.Not.Null);
        Assert.That(entityDto.Id, Is.EqualTo(entity.Id));
        Assert.That(entityDto.CreateDate, Is.EqualTo(entity.CreateDate));
        Assert.That(entityDto.EditDate, Is.EqualTo(entity.EditDate));
        Assert.That(entityDto.Note, Is.EqualTo(entity.Note));
        Assert.That(entityDto.Author, Is.EqualTo(entity.Author));
    }

    [Test]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new MemberNoteDto().PopulateWithRandomData();

        // Act
        var entity = MemberNoteMapper.ToEntity(entityDto);

        // Assert
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity.Id, Is.EqualTo(entityDto.Id));
        Assert.That(entity.CreateDate, Is.EqualTo(entityDto.CreateDate));
        Assert.That(entity.EditDate, Is.EqualTo(entityDto.EditDate));
        Assert.That(entity.Note, Is.EqualTo(entityDto.Note));
        Assert.That(entity.Author, Is.EqualTo(entityDto.Author));
    }
}