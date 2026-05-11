using Discord;
using Discord.Interactions;

namespace ExcelBotCs.Modules.Misc;

public class ComponentInteraction : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("component1", "Component test 1")]
    public async Task Test1()
    {
        // var builder = new ComponentBuilder().WithButton("Healer").WithButton();

        var buttons = new List<ButtonBuilder>();

        var tankButton = new ButtonBuilder("Tank", style: ButtonStyle.Primary);
        var healerButton = new ButtonBuilder("Healer", style: ButtonStyle.Secondary);
        var meleeButton = new ButtonBuilder("Melee", style: ButtonStyle.Success);
        var rangeButton = new ButtonBuilder("Range", style: ButtonStyle.Danger);
        var casterButton = new ButtonBuilder("Caster", style: ButtonStyle.Premium);

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