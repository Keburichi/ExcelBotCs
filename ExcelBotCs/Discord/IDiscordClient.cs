using Discord;
using Discord.WebSocket;
using ExcelBotCs.Modules.TeamFormation;

namespace ExcelBotCs.Discord;

public interface IDiscordBotClient
{
    DiscordSocketClient Client { get; }
    SocketGuild GetExcelGuild();
    SocketGuild? GetGuild(ulong guildId);
    Task<IChannel?> GetChannelAsync(ulong channelId);
    IUser? GetUser(ulong userId);
    Task<IMessageResponse> GetMessageFromUrl(string url);
    Emote? GetEmoteById(ulong id);
    Emote? GetTankEmote();
    Emote? GetHealerEmote();
    Emote? GetMeleeEmote();
    Emote? GetRangedEmote();
    Emote? GetCasterEmote();
    Emote? GetEmoteByRole(Role role);
}