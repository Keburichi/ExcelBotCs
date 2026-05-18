using ExcelBotCs.Mappers.Members;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class MemberMappingExtensionsTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var entity = new Member().PopulateWithRandomData();

        var dto = entity.ToDto();

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(entity.Id);
        dto.DiscordId.ShouldBe(entity.DiscordId);
        dto.DiscordAvatar.ShouldBe(entity.DiscordAvatar);
        dto.DiscordName.ShouldBe(entity.DiscordName);
        dto.LodestoneId.ShouldBe(entity.LodestoneId);
        dto.LodestoneVerificationToken.ShouldBe(entity.LodestoneVerificationToken);
        dto.PlayerName.ShouldBe(entity.PlayerName);
        (dto.Subbed ?? false).ShouldBe(entity.Subbed ?? false);
        (dto.Experience?.Count ?? 0).ShouldBe(entity.Experience?.Count ?? 0);
        (dto.Notes?.Count ?? 0).ShouldBe(entity.Notes?.Count ?? 0);
        dto.Roles.Count.ShouldBe(entity.Roles.Count);
    }

    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var dto = new MemberResponse().PopulateWithRandomData();

        var entity = dto.ToEntity();

        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(dto.Id);
        entity.DiscordId.ShouldBe(dto.DiscordId);
        entity.DiscordAvatar.ShouldBe(dto.DiscordAvatar);
        entity.DiscordName.ShouldBe(dto.DiscordName);
        entity.LodestoneId.ShouldBe(dto.LodestoneId);
        entity.LodestoneVerificationToken.ShouldBe(dto.LodestoneVerificationToken);
        entity.PlayerName.ShouldBe(dto.PlayerName);
        (entity.Subbed ?? false).ShouldBe(dto.Subbed ?? false);
        (entity.Experience?.Count ?? 0).ShouldBe(dto.Experience?.Count ?? 0);
        (entity.Notes?.Count ?? 0).ShouldBe(dto.Notes?.Count ?? 0);
        entity.Roles.Count.ShouldBe(dto.Roles.Count);
    }
}