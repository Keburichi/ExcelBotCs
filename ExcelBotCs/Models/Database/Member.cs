using ExcelBotCs.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace ExcelBotCs.Models.Database;

[BsonIgnoreExtraElements]
public class Member : BaseEntity
{
    public string DiscordId { get; set; }
    public string DiscordName { get; set; }
    public string DiscordAvatar { get; set; }
    public string? PlayerName { get; set; }
    public bool? Subbed { get; set; }
    public string? LodestoneId { get; set; }

    // Token the user must place in their Lodestone Bio to verify ownership
    public string? LodestoneVerificationToken { get; set; }
    
    [BsonIgnore]
    public List<Fight>? Experience { get; set; }
    public List<string> ExperienceIds { get; set; }

    // FFLogs sync tracking
    public DateTime? LastFFLogsSyncTime { get; set; }
    public List<MemberNote>? Notes { get; set; }
    
    [BsonIgnore]
    public List<MemberRole> Roles { get; set; }
    public List<string> RoleIds { get; set; }
    
    public bool? IsAdmin
    {
        get { return Roles != null && Roles.Any(x => x.IsAdmin); }
    }
    
    public bool? IsMember
    {
        // This is true if the user is an admin or has a member role
        get { return Roles != null && (Roles.Any(x => x.IsMember) || IsAdmin.GetValueOrDefault()); }
    }
}