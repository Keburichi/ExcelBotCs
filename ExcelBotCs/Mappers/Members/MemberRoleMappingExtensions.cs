using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;

namespace ExcelBotCs.Mappers.Members;

public static class MemberRoleMappingExtensions
{
    public static MemberRoleDto ToDto(this MemberRole memberRole)
    {
        return new MemberRoleDto
        {
            Id = memberRole.Id,
            DiscordId = memberRole.DiscordId,
            Name = memberRole.Name,
            IsAdmin = memberRole.IsAdmin,
            IsMember = memberRole.IsMember,
            IsDeveloper = memberRole.IsDeveloper
        };
    }

    public static MemberRole ToEntity(this MemberRoleDto memberRole)
    {
        return new MemberRole
        {
            Id = memberRole.Id,
            DiscordId = memberRole.DiscordId,
            Name = memberRole.Name,
            IsAdmin = memberRole.IsAdmin,
            IsMember = memberRole.IsMember,
            IsDeveloper = memberRole.IsDeveloper
        };
    }
}