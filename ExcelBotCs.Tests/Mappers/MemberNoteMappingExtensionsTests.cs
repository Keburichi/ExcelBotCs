using ExcelBotCs.Mappers.Members;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class MemberNoteMappingExtensionsTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var entity = new MemberNote().PopulateWithRandomData();

        var dto = entity.ToDto();

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(entity.Id);
        dto.DateCreated.ShouldBe(entity.DateCreated);
        dto.DateModified.ShouldBe(entity.DateModified);
        dto.Note.ShouldBe(entity.Note);
        dto.Author.ShouldBe(entity.Author);
    }

    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var dto = new NoteResponse().PopulateWithRandomData();

        var entity = dto.ToEntity();

        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(dto.Id);
        entity.DateCreated.ShouldBe(dto.DateCreated);
        entity.DateModified.ShouldBe(dto.DateModified);
        entity.Note.ShouldBe(dto.Note);
        entity.Author.ShouldBe(dto.Author);
    }
}