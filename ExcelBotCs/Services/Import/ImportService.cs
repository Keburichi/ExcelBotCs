using Discord;
using Discord.WebSocket;
using ExcelBotCs.Discord;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Database;

namespace ExcelBotCs.Services.Import;

public class ImportService
{
    private readonly DiscordBotService _discordBotService;
    private readonly MemberRoleService _memberRoleService;
    private readonly MemberService _memberService;

    public ImportService(DiscordBotService discordBotService, MemberRoleService memberRoleService,
        MemberService memberService)
    {
        _discordBotService = discordBotService;
        _memberRoleService = memberRoleService;
        _memberService = memberService;
    }

    public async Task<List<Member>> ImportMembers(ulong guildId = 0)
    {
        var guilds = _discordBotService.Client.Guilds;

        if (guilds.IsNullOrEmpty())
            return new List<Member>();

        var members = new List<Member>();

        // Import all roles
        await ImportRoles();
        
        // If a guild id has been provided, only import that one. Otherwise we go for all servers
        if (guildId != 0)
        {
            var guild = guilds.FirstOrDefault(x => x.Id == guildId);
            if (guild == null)
                return new List<Member>();

            members = await GetGuildMembers(guild);
        }
        else
        {
            foreach (var guild in guilds)
            {
                members = await GetGuildMembers(guild);
            }
        }

        foreach (var member in members)
        {
            // Check if we need to create or update the member
            var dbMember = await _memberService.GetByDiscordId(member.DiscordId);
            
            if(dbMember == null)
                await _memberService.CreateAsync(member);
            else
            {
                member.Id = dbMember.Id;
                await _memberService.UpdateAsync(dbMember.Id, member);
            }
        }

        return members;
    }

    private async Task<List<Member>> GetGuildMembers(SocketGuild guild)
    {
        var members = new List<Member>();
        var memberRoles = await _memberRoleService.GetAsync();

        var guildMembers = guild.Users;
        
        if (guildMembers.IsNullOrEmpty())
            return members;
        
        foreach (var guildMember in guildMembers.Where(x => !x.IsBot))
        {
            if(guildMember is null)
                continue;
            
            var guildRoles = guildMember.Roles.ToList();
            var assignedRoles = guildRoles
                .Select(guildRole => memberRoles.FirstOrDefault(x => x.DiscordId == guildRole.Id)).OfType<MemberRole>()
                .ToList();
        
            members.Add(new Member()
            {
                DiscordId = guildMember.Id.ToString(),
                DiscordName = guildMember.DisplayName,
                DiscordAvatar = $"https://cdn.discordapp.com/avatars/{guildMember.Id}/{guildMember.AvatarId}",
                Roles = assignedRoles
            });
        }

        return members;
    }

    public async Task<List<MemberRole>> ImportRoles()
    {
        var guilds = _discordBotService.Client.Guilds;

        List<MemberRole> roles = new List<MemberRole>();

        foreach (var guild in guilds)
        {
            foreach (var guildRole in guild.Roles)
            {
                roles.Add(new MemberRole()
                {
                    DiscordId = guildRole.Id,
                    Name = guildRole.Name
                });
            }
        }

        foreach (var memberRole in roles)
        {
            // Check if role already exists, if it does we update, otherwise we create
            var role = await _memberRoleService.GetByDiscordId(memberRole.DiscordId);
            if (role != null)
            {
                await _memberRoleService.UpdateAsync(memberRole.Id, memberRole);
            }
            else
            {
                await _memberRoleService.CreateAsync(memberRole);
            }
        }

        return roles;
    }
}