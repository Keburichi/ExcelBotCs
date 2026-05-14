using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class MemberRoleMapperTests
{
    [Fact]
    public void ToDto()
    {
        // Arrange
        var entity = new MemberRole().PopulateWithRandomData();

        // Act
        var entityDto = MemberRoleMapper.ToDto(entity);

        // Assert
        entityDto.ShouldNotBeNull();
        entityDto.Id.ShouldBe(entity.Id);
        entityDto.DiscordId.ShouldBe(entity.DiscordId);
        entityDto.Name.ShouldBe(entity.Name);
        entityDto.IsAdmin.ShouldBe(entity.IsAdmin);
        entityDto.IsMember.ShouldBe(entity.IsMember);
        entityDto.IsDeveloper.ShouldBe(entity.IsDeveloper);
    }

    [Fact]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new MemberRoleDto().PopulateWithRandomData();

        // Act
        var entity = MemberRoleMapper.ToEntity(entityDto);

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(entityDto.Id);
        entity.DiscordId.ShouldBe(entityDto.DiscordId);
        entity.Name.ShouldBe(entityDto.Name);
        entity.IsAdmin.ShouldBe(entityDto.IsAdmin);
        entity.IsMember.ShouldBe(entityDto.IsMember);
        entity.IsDeveloper.ShouldBe(entityDto.IsDeveloper);
    }
}