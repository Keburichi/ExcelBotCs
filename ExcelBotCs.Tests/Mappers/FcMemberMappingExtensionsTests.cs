using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class FcMemberMappingExtensionsTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var entity = new FcMember().PopulateWithRandomData();

        var dto = entity.ToDto();

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(entity.Id);
        dto.Name.ShouldBe(entity.Name);
        dto.Bio.ShouldBe(entity.Bio);
        dto.Avatar.ShouldBe(entity.Avatar);
        dto.CharacterId.ShouldBe(entity.CharacterId);
        dto.FcRank.ShouldBe(entity.FcRank);
        dto.Title.ShouldBe(entity.Title);
        dto.LastSynchronisation.ShouldBe(entity.LastSynchronisation);
    }

    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var dto = new FcMemberDto().PopulateWithRandomData();

        var entity = dto.ToEntity();

        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(dto.Id);
        entity.Name.ShouldBe(dto.Name);
        entity.Bio.ShouldBe(dto.Bio);
        entity.Avatar.ShouldBe(dto.Avatar);
        entity.CharacterId.ShouldBe(dto.CharacterId);
        entity.FcRank.ShouldBe(dto.FcRank);
        entity.Title.ShouldBe(dto.Title);
        entity.LastSynchronisation.ShouldBe(dto.LastSynchronisation);
    }
}