using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ExcelBotCs.Modules.Misc;

[Group("signup", "testing custom signup components")]
public class ComponentInteraction : InteractionModuleBase<SocketInteractionContext>
{
    private readonly DiscordSocketClient _discordSocketClient;

    public ComponentInteraction(DiscordSocketClient discordSocketClient)
    {
        _discordSocketClient = discordSocketClient;
    }

    [SlashCommand("component", "Component test 1")]
    public async Task Test1()
    {
        // await RespondAsync("hello world");
        // var builder = new ComponentBuilder().WithButton("Healer").WithButton();

        var buttons = new List<ButtonBuilder>();

        // var remotes = await _discordSocketClient.GetApplicationEmotesAsync();
        // Emote? tankEmote = await _discordSocketClient.GetApplicationEmoteAsync(1380979172423499846);
        // Emote? healEmote = await _discordSocketClient.GetApplicationEmoteAsync(1380979170787721368);
        // Emote? meleeEmote = await _discordSocketClient.GetApplicationEmoteAsync(873621778214318091);
        // Emote? rangeEmote = await _discordSocketClient.GetApplicationEmoteAsync(873621778453368895);
        // Emote? casterEmote = await _discordSocketClient.GetApplicationEmoteAsync(873621778566635540);

        var tankButton = new ButtonBuilder("Tank", "signup-tank", ButtonStyle.Primary);

        // if (tankEmote != null)
        //     tankButton.WithEmote(tankEmote);

        var healerButton = new ButtonBuilder("Healer", "signup-healer", ButtonStyle.Primary);

        // if (healEmote != null)
        //     healerButton.WithEmote(healEmote);

        var meleeButton = new ButtonBuilder("Melee", "signup-melee", ButtonStyle.Primary);

        // if (meleeEmote != null)
        //     meleeButton.WithEmote(meleeEmote);

        var rangeButton = new ButtonBuilder("Range", "signup-range", ButtonStyle.Primary);

        // if (rangeEmote != null)
        //     rangeButton.WithEmote(rangeEmote);

        var casterButton = new ButtonBuilder("Caster", "signup-caster", ButtonStyle.Primary);

        // if (casterEmote != null)
        //     casterButton.WithEmote(casterEmote);

        buttons.Add(tankButton);
        buttons.Add(healerButton);
        buttons.Add(meleeButton);
        buttons.Add(rangeButton);
        buttons.Add(casterButton);

        var builder2 = new ComponentBuilderV2()
            .WithTextDisplay(new TextDisplayBuilder("Test sign up component"))
            .WithActionRow(buttons)
            .Build();

        await RespondAsync(components: builder2);
    }
}