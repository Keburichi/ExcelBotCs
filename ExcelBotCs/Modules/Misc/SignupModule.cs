using Discord.Interactions;
using ExcelBotCs.Modules.TeamFormation;
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

        if (fcEvent.UsesCustomButtons)
        {
            var buttonConfig = fcEvent.SignupButtonConfigs!.FirstOrDefault(b => b.Slug == slug);
            if (buttonConfig == null)
            {
                await FollowupAsync("Unknown button.", ephemeral: true);
                return;
            }

            await _eventService.HandleSignupAsync(eventId, slug, Context.User.Id);
        }
        else
        {
            var role = slug switch
            {
                "tank"   => (Role?)Role.Tank,
                "healer" => Role.Healer,
                "melee"  => Role.Melee,
                "ranged" => Role.Ranged,
                "caster" => Role.Caster,
                _        => null
            };

            if (role is null)
            {
                await FollowupAsync("Unknown role.", ephemeral: true);
                return;
            }

            await _eventService.HandleSignupAsync(eventId, role.Value, Context.User.Id);
        }

        fcEvent = await _eventService.GetAsync(eventId);
        if (fcEvent != null && !string.IsNullOrEmpty(fcEvent.DiscordMessageId))
            await _discordMessageService.UpdateSignupMessage(fcEvent);

        await FollowupAsync("Your signup has been received!", ephemeral: true);
    }
}
