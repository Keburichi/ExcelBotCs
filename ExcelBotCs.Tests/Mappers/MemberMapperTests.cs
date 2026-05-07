using ExcelBotCs.Mappers;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class MemberMapperTests
{
    [Fact]
    public void ToDto()
    {
        // Arrange
        var entity = new Member().PopulateWithRandomData();

        // Act
        var entityDto = MemberMapper.ToDto(entity);

        // Assert
        entityDto.ShouldNotBeNull();
        entityDto.Id.ShouldBe(entity.Id);
        entityDto.DiscordId.ShouldBe(entity.DiscordId);
        entityDto.DiscordAvatar.ShouldBe(entity.DiscordAvatar);
        entityDto.DiscordName.ShouldBe(entity.DiscordName);
        entityDto.LodestoneId.ShouldBe(entity.LodestoneId);
        entityDto.LodestoneVerificationToken.ShouldBe(entity.LodestoneVerificationToken);
        entityDto.PlayerName.ShouldBe(entity.PlayerName);
        (entityDto.Subbed ?? false).ShouldBe(entity.Subbed ?? false);
        (entityDto.Experience?.Count ?? 0).ShouldBe(entity.Experience?.Count ?? 0);
        (entityDto.Notes?.Count ?? 0).ShouldBe(entity.Notes?.Count ?? 0);
        entityDto.Roles.Count.ShouldBe(entity.Roles.Count);
    }

    [Fact]
    public void ToEntity()
    {
        // Arrange
        var entityDto = new MemberDto().PopulateWithRandomData();

        // Act
        var entity = MemberMapper.ToEntity(entityDto);

        // Assert
        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(entityDto.Id);
        entity.DiscordId.ShouldBe(entityDto.DiscordId);
        entity.DiscordAvatar.ShouldBe(entityDto.DiscordAvatar);
        entity.DiscordName.ShouldBe(entityDto.DiscordName);
        entity.LodestoneId.ShouldBe(entityDto.LodestoneId);
        entity.LodestoneVerificationToken.ShouldBe(entityDto.LodestoneVerificationToken);
        entity.PlayerName.ShouldBe(entityDto.PlayerName);
        (entity.Subbed ?? false).ShouldBe(entityDto.Subbed ?? false);
        (entity.Experience?.Count ?? 0).ShouldBe(entityDto.Experience?.Count ?? 0);
        (entity.Notes?.Count ?? 0).ShouldBe(entityDto.Notes?.Count ?? 0);
        entity.Roles.Count.ShouldBe(entityDto.Roles.Count);
    }
}