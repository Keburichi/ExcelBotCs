using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class FcMemberMapperTests
{
    [Fact]
    public void ToDto()
    {
        // Arrange
        var entity = new FcMember().PopulateWithRandomData();

        // Act
        var entityDto = FcMemberMapper.ToDto(entity);

        // Assert
        entityDto.ShouldNotBeNull();
        entityDto.Id.ShouldBe(entity.Id);
        entityDto.Name.ShouldBe(entity.Name);
        entityDto.Bio.ShouldBe(entity.Bio);
        entityDto.Avatar.ShouldBe(entity.Avatar);
        entityDto.CharacterId.ShouldBe(entity.CharacterId);
        entityDto.FcRank.ShouldBe(entity.FcRank);
        entityDto.Title.ShouldBe(entity.Title);
        entityDto.LastSynchronisation.ShouldBe(entity.LastSynchronisation);
    }

    [Fact]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new FcMemberDto().PopulateWithRandomData();

        // Act
        var entity = FcMemberMapper.ToEntity(entityDto);

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(entityDto.Id);
        entity.Name.ShouldBe(entityDto.Name);
        entity.Bio.ShouldBe(entityDto.Bio);
        entity.Avatar.ShouldBe(entityDto.Avatar);
        entity.CharacterId.ShouldBe(entityDto.CharacterId);
        entity.FcRank.ShouldBe(entityDto.FcRank);
        entity.Title.ShouldBe(entityDto.Title);
        entity.LastSynchronisation.ShouldBe(entityDto.LastSynchronisation);
    }
}