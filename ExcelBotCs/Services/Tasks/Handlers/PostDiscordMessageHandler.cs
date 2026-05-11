using System.Text.Json;
using Discord;
using Discord.WebSocket;
using ExcelBotCs.Models.Tasks;

namespace ExcelBotCs.Services.Tasks.Handlers;

public class PostDiscordMessageHandler : IBotTaskHandler
{
    private readonly DiscordSocketClient _discordClient;

    public PostDiscordMessageHandler(DiscordSocketClient discordClient)
    {
        _discordClient = discordClient;
    }

    public string TaskType => BotTaskTypes.PostDiscordMessage;

    public async Task ExecuteAsync(string payload, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Deserialize<PostDiscordMessagePayload>(payload)!;
        var channel = await _discordClient.GetChannelAsync(data.ChannelId) as IMessageChannel;
        if (channel == null) return;
        await channel.SendMessageAsync(data.Message);
    }
}
