using System.Text.Json;
using ExcelBotCs.Models.Tasks;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Services.Tasks.Handlers;

public class PostEventSignupHandler : IBotTaskHandler
{
    private readonly IEventService _eventService;

    public PostEventSignupHandler(IEventService eventService)
    {
        _eventService = eventService;
    }

    public string TaskType => BotTaskTypes.PostEventSignup;

    public async Task ExecuteAsync(string payload, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Deserialize<PostEventSignupPayload>(payload)!;
        await _eventService.HandleSignupAsync(data.EventId, data.Role, data.DiscordUserId);
    }
}
