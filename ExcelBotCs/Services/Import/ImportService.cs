using Discord.WebSocket;
using ExcelBotCs.Discord;
using ExcelBotCs.Extensions;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Models.Database;
using ExcelBotCs.Services.API.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Services.Import;

public class ImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly IDiscordBotClient _discordClient;
    private readonly IMemberRoleService _memberRoleService;
    private readonly IMemberService _memberService;
    private readonly IOptions<DiscordBotOptions> _options;

    public ImportService(ILogger<ImportService> logger, IDiscordBotClient discordClient,
        IMemberRoleService memberRoleService,
        IMemberService memberService, IOptions<DiscordBotOptions> options)
    {
        _logger = logger;
        _discordClient = discordClient;
        _memberRoleService = memberRoleService;
        _memberService = memberService;
        _options = options;
    }

    public async Task<List<Member>> ImportMembers()
    {
        return await ImportMembers(_options.Value.GuildId);
    }

    public async Task<List<Member>> ImportMembers(ulong guildId)
    {
        _logger.LogInformation("Importing discord members");

        var guild = _discordClient.GetGuild(guildId);
        if (guild is null)
            return new List<Member>();

        await ImportRoles(guildId);

        var members = await GetGuildMembers(guild);

        foreach (var member in members)
        {
            var dbMember = await _memberService.GetByDiscordId(member.DiscordId);

            if (dbMember == null)
                await _memberService.CreateAsync(member);
            else
            {
                member.Id = dbMember.Id;
                member.Roles = dbMember.Roles;
                member.Subbed = dbMember.Subbed;
                member.LodestoneId = dbMember.LodestoneId;
                member.LodestoneVerificationToken = dbMember.LodestoneVerificationToken;
                member.Notes = dbMember.Notes;
                member.PlayerName = dbMember.PlayerName;
                member.LastFFLogsSyncTime = dbMember.LastFFLogsSyncTime;
                member.ExperienceIds = dbMember.ExperienceIds;

                await _memberService.UpdateAsync(dbMember.Id, member);
                await _memberService.UpdateDiscordRoles(dbMember.Id, member.RoleIds);
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
            if (guildMember is null)
                continue;

            var guildRoles = guildMember.Roles.ToList();
            var assignedRoles = guildRoles
                .Select(guildRole => memberRoles.FirstOrDefault(x => x.DiscordId == guildRole.Id.ToString()))
                .OfType<MemberRole>()
                .ToList();

            if (assignedRoles.Any(x => x.IsMember))
                members.Add(new Member
                {
                    DiscordId = guildMember.Id.ToString(),
                    DiscordName = guildMember.DisplayName,
                    DiscordAvatar = $"https://cdn.discordapp.com/avatars/{guildMember.Id}/{guildMember.AvatarId}",
                    RoleIds = assignedRoles.Select(x => x.Id.ToString()).ToList()
                });
        }

        return members;
    }

    public async Task<List<MemberRole>> ImportRoles(ulong guildId = 0)
    {
        _logger.LogInformation("Importing discord roles");

        var guild = guildId != 0 ? _discordClient.GetGuild(guildId) : _discordClient.GetExcelGuild();
        var roles = new List<MemberRole>();

        if (guild is not null)
            foreach (var guildRole in guild.Roles)
                roles.Add(new MemberRole { DiscordId = guildRole.Id.ToString(), Name = guildRole.Name });

        foreach (var memberRole in roles)
        {
            var role = await _memberRoleService.GetByDiscordId(memberRole.DiscordId);
            if (role != null)
            {
                memberRole.IsAdmin = role.IsAdmin;
                memberRole.IsMember = role.IsMember;
                await _memberRoleService.UpdateAsync(memberRole.Id, memberRole);
            }
            else
            {
                if (_options.Value.AdminRoleIds.Any(x => x.ToString() == memberRole.DiscordId))
                    memberRole.IsAdmin = true;

                if (_options.Value.MemberRoleIds.Any(x => x.ToString() == memberRole.DiscordId))
                    memberRole.IsMember = true;

                await _memberRoleService.CreateAsync(memberRole);
            }
        }

        return roles;
    }
}
