using Discord.Interactions;
using ExcelBotCs.Models.Tasks;
using ExcelBotCs.Modules.TeamFormation;
using ExcelBotCs.Services.API.Interfaces;

namespace ExcelBotCs.Modules.Misc;

public class SignupModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IBotTaskService _botTaskService;

    public SignupModule(IBotTaskService botTaskService)
    {
        _botTaskService = botTaskService;
    }

    [ComponentInteraction("*-signup-*")]
    public async Task HandleSignupButton(string eventId, string roleName)
    {
        await DeferAsync(ephemeral: true);

        var role = roleName switch
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

        await _botTaskService.EnqueueAsync(BotTaskTypes.PostEventSignup, new PostEventSignupPayload
        {
            EventId = eventId,
            Role = role.Value,
            DiscordUserId = Context.User.Id
        });

        await FollowupAsync("Your signup has been received!", ephemeral: true);
    }
}
