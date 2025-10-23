using Discord.WebSocket;
using ExcelBotCs.Discord;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Import;

public class ImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly IMemberRoleService _memberRoleService;
    private readonly IMemberService _memberService;
    private readonly IOptions<DiscordBotOptions> _options;

    public ImportService(ILogger<ImportService> logger, DiscordSocketClient discordSocketClient,
        IMemberRoleService memberRoleService,
        IMemberService memberService, IOptions<DiscordBotOptions> options)
    {
        _logger = logger;
        _discordSocketClient = discordSocketClient;
        _memberRoleService = memberRoleService;
        _memberService = memberService;
        _options = options;
    }

    public async Task<List<Member>> ImportMembers()
    {
        return await ImportMembers(_options.Value.GuildId);
    }

    public async Task<List<Member>> ImportMembers(ulong guildId = 0)
    {
        _logger.LogInformation("Importing discord members");

        var guilds = _discordSocketClient.Guilds;

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

            if (dbMember == null)
                await _memberService.CreateAsync(member);
            else
            {
                member.Id = dbMember.Id;

                // Don't override some properties to avoid accidental removals
                member.Roles = dbMember.Roles;
                member.Subbed = dbMember.Subbed;
                member.LodestoneId = dbMember.LodestoneId;
                member.LodestoneVerificationToken = dbMember.LodestoneVerificationToken;
                member.Experience = dbMember.Experience;

                await _memberService.UpdateAsync(dbMember.Id, member);
            }
        }

        return members;
    }

    private async Task<List<Member>> GetGuildMembers(SocketGuild guild)
    {
        var members = new List<Member>();
        // var memberRoles = await _memberRoleService.GetAsync();

        var guildMembers = guild.Users;

        if (guildMembers.IsNullOrEmpty())
            return members;

        foreach (var guildMember in guildMembers.Where(x => !x.IsBot))
        {
            if (guildMember is null)
                continue;

            var guildRoles = guildMember.Roles.ToList();
            // var assignedRoles = guildRoles
            //     .Select(guildRole => memberRoles.FirstOrDefault(x => x.DiscordId == guildRole.Id.ToString())).OfType<MemberRole>()
            //     .ToList();

            members.Add(new Member()
            {
                DiscordId = guildMember.Id.ToString(),
                DiscordName = guildMember.DisplayName,
                DiscordAvatar = $"https://cdn.discordapp.com/avatars/{guildMember.Id}/{guildMember.AvatarId}",
                // Roles = assignedRoles,
                RoleIds = guildRoles.Select(x => x.Id.ToString()).ToList()
            });
        }

        return members;
    }

    public async Task<List<MemberRole>> ImportRoles(ulong guildId = 0)
    {
        _logger.LogInformation("Importing discord roles");

        var guilds = _discordSocketClient.Guilds;

        List<MemberRole> roles = new List<MemberRole>();

        if (guildId != 0)
        {
            var guild = guilds.FirstOrDefault(x => x.Id == guildId);

            foreach (var guildRole in guild.Roles)
            {
                roles.Add(new MemberRole()
                {
                    DiscordId = guildRole.Id.ToString(),
                    Name = guildRole.Name
                });
            }
        }
        else
        {
            foreach (var guild in guilds)
            {
                foreach (var guildRole in guild.Roles)
                {
                    roles.Add(new MemberRole()
                    {
                        DiscordId = guildRole.Id.ToString(),
                        Name = guildRole.Name
                    });
                }
            }
        }

        foreach (var memberRole in roles)
        {
            // Check if role already exists, if it does we update, otherwise we create
            var role = await _memberRoleService.GetByDiscordId(memberRole.DiscordId);
            if (role != null)
            {
                // don't update certain properties
                memberRole.IsAdmin = role.IsAdmin;
                memberRole.IsMember = role.IsMember;

                await _memberRoleService.UpdateAsync(memberRole.Id, memberRole);
            }
            else
            {
                // automatically assign the member and admin flags for the initial import
                if(_options.Value.AdminRoleIds.Any(x => x.ToString() == memberRole.DiscordId))
                    memberRole.IsAdmin = true;

                if(_options.Value.MemberRoleIds.Any(x => x.ToString() == memberRole.DiscordId))
                    memberRole.IsMember = true;

                await _memberRoleService.CreateAsync(memberRole);
            }
        }

        return roles;
    }
}