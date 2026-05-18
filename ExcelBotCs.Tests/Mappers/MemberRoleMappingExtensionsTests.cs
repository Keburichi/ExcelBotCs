using ExcelBotCs.Mappers.Members;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;
using ExcelBotCs.TestFramework.Utils;

namespace ExcelBotCs.Tests.Mappers;

public class MemberRoleMappingExtensionsTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var entity = new MemberRole().PopulateWithRandomData();

        var dto = entity.ToDto();

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(entity.Id);
        dto.DiscordId.ShouldBe(entity.DiscordId);
        dto.Name.ShouldBe(entity.Name);
        dto.IsAdmin.ShouldBe(entity.IsAdmin);
        dto.IsMember.ShouldBe(entity.IsMember);
        dto.IsDeveloper.ShouldBe(entity.IsDeveloper);
    }

    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var dto = new MemberRoleDto().PopulateWithRandomData();

        var entity = dto.ToEntity();

        entity.ShouldNotBeNull();
        entity.Id.ShouldBe(dto.Id);
        entity.DiscordId.ShouldBe(dto.DiscordId);
        entity.Name.ShouldBe(dto.Name);
        entity.IsAdmin.ShouldBe(dto.IsAdmin);
        entity.IsMember.ShouldBe(dto.IsMember);
        entity.IsDeveloper.ShouldBe(dto.IsDeveloper);
    }
}