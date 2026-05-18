using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using ExcelBotCs.Models.Config;
using ExcelBotCs.Modules.TeamFormation;
using Microsoft.Extensions.Options;

namespace ExcelBotCs.Discord;

public class DiscordClient : IDiscordBotClient
{
    public DiscordSocketClient Client { get; }
    private readonly DiscordBotOptions _options;
    private readonly ILogger<DiscordClient> _logger;

    public DiscordClient(DiscordSocketClient client, IOptions<DiscordBotOptions> options, ILogger<DiscordClient> logger)
    {
        Client = client;
        _options = options.Value;
        _logger = logger;

        Client.Log += HandleLog;
    }

    public SocketGuild GetExcelGuild()
    {
        return Client.GetGuild(_options.GuildId)
               ?? throw new InvalidOperationException($"Guild {_options.GuildId} not found — bot may not be ready yet");
    }

    public SocketGuild? GetGuild(ulong guildId)
    {
        return Client.GetGuild(guildId);
    }

    public async Task<IChannel?> GetChannelAsync(ulong channelId)
    {
        return await (Client as IDiscordClient).GetChannelAsync(channelId);
    }

    public IUser? GetUser(ulong userId)
    {
        return Client.GetUser(userId);
    }

    public async Task<IMessageResponse> GetMessageFromUrl(string url)
    {
        var match = Regex.Match(url, @"discord\.com/channels/(?<guildId>\d+)/(?<channelId>\d+)/(?<messageId>\d+)");

        if (!match.Success)
            return new NotFoundUrlMessageResponse();

        var guildId = ulong.Parse(match.Groups["guildId"].Value);
        var channelId = ulong.Parse(match.Groups["channelId"].Value);
        var messageId = ulong.Parse(match.Groups["messageId"].Value);

        return Client.GetGuild(guildId)?.GetChannel(channelId) is not ITextChannel channel
            ? new NotValidUrlMessageResponse()
            : new SuccessMessageResponse(await channel.GetMessageAsync(messageId));
    }

    public Emote? GetEmoteById(ulong id)
    {
        return Client.Guilds.SelectMany(g => g.Emotes).FirstOrDefault(e => e.Id == id);
    }

    public Emote? GetTankEmote()
    {
        return GetEmoteById(Constants.TankRoleEmoteId);
    }

    public Emote? GetHealerEmote()
    {
        return GetEmoteById(Constants.HealerRoleEmoteId);
    }

    public Emote? GetMeleeEmote()
    {
        return GetEmoteById(Constants.MeleeRoleEmoteId);
    }

    public Emote? GetRangedEmote()
    {
        return GetEmoteById(Constants.RangedRoleEmoteId);
    }

    public Emote? GetCasterEmote()
    {
        return GetEmoteById(Constants.CasterRoleEmoteId);
    }

    public Emote? GetEmoteByRole(Role role)
    {
        return role switch
        {
            Role.Tank => GetTankEmote(),
            Role.Healer => GetHealerEmote(),
            Role.Melee => GetMeleeEmote(),
            Role.Caster => GetCasterEmote(),
            Role.Ranged => GetRangedEmote(),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    private Task HandleLog(LogMessage log)
    {
        var level = log.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Debug
        };

        _logger.Log(level, log.Exception, "{Source}: {Message}", log.Source, log.Message);
        return Task.CompletedTask;
    }
}