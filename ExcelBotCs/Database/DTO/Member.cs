using ExcelBotCs.Attributes;

namespace ExcelBotCs.Database.DTO;

public class Member : BaseEntity
{
    [RequiresMemberRole] 
    public string DiscordId { get; set; }

    [RequiresMemberRole] 
    public string DiscordName { get; set; }

    [RequiresMemberRole] 
    public string DiscordAvatar { get; set; }
    public string? PlayerName { get; set; }

    [RequiresAdminRole] 
    public bool? Subbed { get; set; }

    [RequiresMemberRole] 
    public string? LodestoneId { get; set; }

    // Token the user must place in their Lodestone Bio to verify ownership
    [RequiresMemberRole]
    public string? LodestoneVerificationToken { get; set; }

    [RequiresMemberRole] 
    public List<Fight>? Experience { get; set; }

    [RequiresAdminRole] 
    public List<MemberNote>? Notes { get; set; }

    [RequiresMemberRole] 
    public List<MemberRole> Roles { get; set; }

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
}