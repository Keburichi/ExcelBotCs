namespace ExcelBotCs.Models.Tasks;

public class PostDiscordMessagePayload
{
    public ulong ChannelId { get; set; }
    public string Message { get; set; } = string.Empty;
}
