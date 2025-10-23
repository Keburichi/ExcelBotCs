using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class MemberRoleMapper
{
    public static MemberRoleDto ToDto(MemberRole memberRole)
    {
        return new MemberRoleDto()
        {
            Id = memberRole.Id,
            DiscordId = memberRole.DiscordId,
            Name = memberRole.Name,
            IsAdmin = memberRole.IsAdmin,
            IsMember = memberRole.IsMember,
        };
    }

    public static MemberRole ToEntity(MemberRoleDto memberRole)
    {
        return new MemberRole()
        {
            Id = memberRole.Id,
            DiscordId = memberRole.DiscordId,
            Name = memberRole.Name,
            IsAdmin = memberRole.IsAdmin,
            IsMember = memberRole.IsMember,
        };
    }
}