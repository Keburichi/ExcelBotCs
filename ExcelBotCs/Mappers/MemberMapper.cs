using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO;

namespace ExcelBotCs.Mappers;

public static class MemberMapper
{
    public static MemberDto ToDto(Member member)
    {
        return new MemberDto()
        {
            Id = member.Id,
            DiscordId = member.DiscordId,
            DiscordAvatar = member.DiscordAvatar,
            DiscordName = member.DiscordName,
            LodestoneId = member.LodestoneId,
            LodestoneVerificationToken = member.LodestoneVerificationToken,
            Experience = member.Experience,
            Notes = member.Notes,
            PlayerName =  member.PlayerName,
            Subbed = member.Subbed,
            Roles = member.Roles.Select(MemberRoleMapper.ToDto).ToList()
        };
    }

    public static Member ToEntity(MemberDto member)
    {
        return new Member()
        {
            Id = member.Id,
            DiscordId = member.DiscordId,
            DiscordAvatar = member.DiscordAvatar,
            DiscordName = member.DiscordName,
            LodestoneId = member.LodestoneId,
            LodestoneVerificationToken = member.LodestoneVerificationToken,
            Experience = member.Experience,
            Notes = member.Notes,
            PlayerName =  member.PlayerName,
            Subbed = member.Subbed,
            Roles = member.Roles.Select(MemberRoleMapper.ToEntity).ToList()
        };
    }
}