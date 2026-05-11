using System.Text.Json;
using ExcelBotCs.Models.Tasks;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;

namespace ExcelBotCs.Services.Tasks.Handlers;

public class UpdateSignupMessageHandler : IBotTaskHandler
{
    private readonly IEventService _eventService;
    private readonly IDiscordMessageService _discordMessageService;

    public UpdateSignupMessageHandler(IEventService eventService, IDiscordMessageService discordMessageService)
    {
        _eventService = eventService;
        _discordMessageService = discordMessageService;
    }

    public string TaskType => BotTaskTypes.UpdateSignupMessage;

    public async Task ExecuteAsync(string payload, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Deserialize<UpdateSignupMessagePayload>(payload)!;
        var fcEvent = await _eventService.GetAsync(data.EventId);
        if (fcEvent == null) return;
        await _discordMessageService.UpdateSignupMessage(fcEvent);
    }
}
