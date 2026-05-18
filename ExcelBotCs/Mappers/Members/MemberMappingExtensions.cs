using ExcelBotCs.Mappers.Fights;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Models.DTO.Members;

namespace ExcelBotCs.Mappers.Members;

public static class MemberMappingExtensions
{
    public static MemberResponse ToDto(this Member member)
    {
        return new MemberResponse
        {
            Id = member.Id,
            DiscordId = member.DiscordId,
            DiscordAvatar = member.DiscordAvatar,
            DiscordName = member.DiscordName,
            LodestoneId = member.LodestoneId,
            LodestoneVerificationToken = member.LodestoneVerificationToken,
            Experience = member.Experience?.Select(f => f.ToDto()).ToList(),
            Notes = member.Notes?.Select(n => n.ToDto()).ToList(),
            PlayerName = member.PlayerName,
            Subbed = member.Subbed,
            Roles = member.Roles.Select(r => r.ToDto()).ToList()
        };
    }

    public static Member ToEntity(this MemberResponse member)
    {
        return new Member
        {
            Id = member.Id,
            DiscordId = member.DiscordId,
            DiscordAvatar = member.DiscordAvatar,
            DiscordName = member.DiscordName,
            LodestoneId = member.LodestoneId,
            LodestoneVerificationToken = member.LodestoneVerificationToken,
            Experience = member.Experience?.Select(f => f.ToEntity()).ToList(),
            Notes = member.Notes?.Select(n => n.ToEntity()).ToList(),
            PlayerName = member.PlayerName,
            Subbed = member.Subbed,
            Roles = member.Roles.Select(r => r.ToEntity()).ToList()
        };
    }
}