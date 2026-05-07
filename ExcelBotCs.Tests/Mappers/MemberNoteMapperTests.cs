using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class MemberNoteMapperTests
{
    [Fact]
    public void ToDto()
    {
        // Arrange
        var entity = new MemberNote().PopulateWithRandomData();

        // Act
        var entityDto = MemberNoteMapper.ToDto(entity);

        // Assert
        entityDto.ShouldNotBeNull();
        entityDto.Id.ShouldBe(entity.Id);
        entityDto.DateCreated.ShouldBe(entity.DateCreated);
        entityDto.DateModified.ShouldBe(entity.DateModified);
        entityDto.Note.ShouldBe(entity.Note);
        entityDto.Author.ShouldBe(entity.Author);
    }

    [Fact]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new MemberNoteDto().PopulateWithRandomData();

        // Act
        var entity = MemberNoteMapper.ToEntity(entityDto);

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(entityDto.Id);
        entity.DateCreated.ShouldBe(entityDto.DateCreated);
        entity.DateModified.ShouldBe(entityDto.DateModified);
        entity.Note.ShouldBe(entityDto.Note);
        entity.Author.ShouldBe(entityDto.Author);
    }
}