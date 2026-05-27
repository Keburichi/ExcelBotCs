using ExcelBotCs.Attributes;

namespace ExcelBotCs.Models.DTO.Members;

public class MemberResponse : BaseDto
{
    [RequiresMemberRole] public string DiscordId { get; set; }

    [RequiresMemberRole] public string DiscordName { get; set; }

    [RequiresMemberRole] public string DiscordAvatar { get; set; }

    public string? PlayerName { get; set; }

    [RequiresMemberRole] public bool? Subbed { get; set; }

    [RequiresMemberRole] public string? LodestoneId { get; set; }

    // Token the user must place in their Lodestone Bio to verify ownership
    [RequiresMemberRole] public string? LodestoneVerificationToken { get; set; }

    [RequiresAdminRole] public List<FightDto>? Experience { get; set; }

    [RequiresAdminRole] public List<NoteResponse>? Notes { get; set; }

    [RequiresMemberRole] public List<MemberRoleDto> Roles { get; set; }

    [RequiresMemberRole]
    public bool? IsAdmin
    {
        get { return Roles != null && Roles.Any(x => x.IsAdmin); }
    }

    [RequiresMemberRole]
    public bool? IsMember
    {
        // This is true if the user is an admin or has a member role
        get { return Roles != null && (Roles.Any(x => x.IsMember) || IsAdmin.GetValueOrDefault()); }
    }

    [RequiresMemberRole]
    public bool? IsDeveloper
    {
        get { return Roles != null && Roles.Any(x => x.IsDeveloper); }
    }
}