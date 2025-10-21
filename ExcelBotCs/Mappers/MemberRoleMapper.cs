using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class MemberRoleMapper
{
    public static MemberRoleDto ToDto(this MemberRole role)
    {
        if (role is null)
            return null;

        return new MemberRoleDto()
        {
            Name = role.Name,
            DiscordId = role.DiscordId,
            IsAdmin = role.IsAdmin,
            IsMember = role.IsMember,
        };
    }
}