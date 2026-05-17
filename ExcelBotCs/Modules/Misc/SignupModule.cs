using Discord.Interactions;
using ExcelBotCs.Services.API.Interfaces;
using ExcelBotCs.Services.Discord.Interfaces;

namespace ExcelBotCs.Modules.Misc;

public class SignupModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IEventService _eventService;
    private readonly IDiscordMessageService _discordMessageService;

    public SignupModule(IEventService eventService, IDiscordMessageService discordMessageService)
    {
        _eventService = eventService;
        _discordMessageService = discordMessageService;
    }

    [ComponentInteraction("*-signup-*")]
    public async Task HandleSignupButton(string eventId, string slug)
    {
        await DeferAsync(ephemeral: true);

        var fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent == null)
        {
            await FollowupAsync("This event no longer exists.", ephemeral: true);
            return;
        }

        if (!fcEvent.AvailableForSignup || fcEvent.IsArchived)
        {
            await FollowupAsync("Signups are closed for this event.", ephemeral: true);
            return;
        }

        var buttonConfig = fcEvent.SignupButtonConfigs!.FirstOrDefault(b => b.Slug == slug);
        if (buttonConfig == null)
        {
            await FollowupAsync("Unknown button.", ephemeral: true);
            return;
        }

        await _eventService.HandleSignupAsync(eventId, slug, Context.User.Id);

        fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent != null && !string.IsNullOrEmpty(fcEvent.SignupPostId))
            await _discordMessageService.UpdateSignupMessage(fcEvent);

        await FollowupAsync("Your signup has been received!", ephemeral: true);
    }
}