using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class MemberMapper
{
    public static MemberDto ToDto(this Member member, List<MemberRole> roles, List<MemberNote> notes)
    {
        if (member is null)
            return null;

        var rolesDtos = roles?.Select(MemberRoleMapper.ToDto).ToList();

        return new MemberDto()
        {
            Id = member.Id,
            Roles = rolesDtos,
            DiscordAvatar = member.DiscordAvatar,
            DiscordName = member.DiscordName,
            DiscordId = member.DiscordId,
            
        };
    }
}